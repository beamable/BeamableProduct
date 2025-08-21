import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { BeamBase } from '@/core/BeamBase';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { ContentStorage } from '@/platform/types/ContentStorage';
import { ClientContentInfoJson, ContentType } from '@/__generated__/schemas';
import {
  type ContentManifestChecksum,
  ContentService,
} from '@/services/ContentService';

// In-memory ContentStorage mock
const makeMemoryContentStorage = () => {
  const store = new Map<string, any>();
  return {
    set: vi.fn(async (key: string, value: any) => {
      if (value === undefined) {
        store.delete(key);
      } else {
        store.set(key, value);
      }
    }),
    get: vi.fn(async (key: string) => store.get(key)),
    has: vi.fn(async (key: string) => store.has(key)),
    del: vi.fn(async (key: string) => {
      store.delete(key);
    }),
    clear: vi.fn(async () => {
      store.clear();
    }),
  } as unknown as ContentStorage & { _dump: () => Map<string, any> };
};

// Mock defaults to avoid filesystem IO from NodeContentStorage
const memoryStorage = makeMemoryContentStorage();
vi.mock('@/defaults', () => ({
  defaultContentStorage: vi.fn(async () => memoryStorage),
}));

// API mocks for content endpoints
const apiMocks = {
  checksum: vi.fn(),
  manifest: vi.fn(),
};

vi.mock('@/__generated__/apis', () => ({
  contentGetManifestChecksumBasic: (...args: any[]) =>
    apiMocks.checksum(...args),
  contentGetManifestPublicJsonBasic: (...args: any[]) =>
    apiMocks.manifest(...args),
}));

describe('ContentService', () => {
  const cid = 'test-cid';
  const pid = 'test-pid';
  const manifestId = 'global';
  const entriesKey = `${cid}.${pid}.${manifestId}.entries`;
  const checksumKey = `${cid}.${pid}.${manifestId}.checksum`;

  const swordEntry: ClientContentInfoJson = {
    contentId: 'items.sword',
    checksum: 'abc123',
    uri: '/content/items/sword.json',
    version: '1',
    tags: ['base'],
    type: ContentType.Content,
  };
  const shieldEntry: ClientContentInfoJson = {
    contentId: 'items.shield',
    checksum: 'def456',
    uri: '/content/items/shield.json',
    version: '1',
    tags: ['base'],
    type: ContentType.Content,
  };
  const manifestEntries: ClientContentInfoJson[] = [swordEntry, shieldEntry];

  const mockRequester: HttpRequester = {
    baseUrl: '',
    defaultHeaders: {},
    request: vi.fn(async ({ url }: { url: string }) => {
      if (url.includes('sword'))
        return {
          status: 200,
          headers: {},
          body: { id: 'items.sword', name: 'Sword' },
        };
      if (url.includes('shield'))
        return {
          status: 200,
          headers: {},
          body: { id: 'items.shield', name: 'Shield' },
        };
      return { status: 404, headers: {}, body: undefined } as any;
    }),
  } as any;

  const makeBeam = (): BeamBase =>
    ({
      cid,
      pid,
      requester: mockRequester,
    }) as unknown as BeamBase;

  const makeService = () => new ContentService({ beam: makeBeam() });

  const clearStaticCaches = (svc: ContentService) => {
    for (const k of Object.keys(svc.contentsCache))
      delete (svc.contentsCache as any)[k];
    for (const k of Object.keys(svc.manifestEntriesCache))
      delete (svc.manifestEntriesCache as any)[k];
    for (const k of Object.keys(svc.manifestChecksumsCache))
      delete (svc.manifestChecksumsCache as any)[k];
  };

  beforeEach(() => {
    vi.clearAllMocks();
    memoryStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getManifestEntries', () => {
    it('fetches from API when not cached or stored, and caches results', async () => {
      const svc = makeService();
      clearStaticCaches(svc);

      apiMocks.manifest.mockResolvedValue({
        status: 200,
        headers: {},
        body: { entries: manifestEntries },
      });

      const result = await svc.getManifestEntries({ manifestId });

      expect(result).toEqual(manifestEntries);
      expect(apiMocks.manifest).toHaveBeenCalled();
      expect(memoryStorage.set).toHaveBeenCalledWith(
        entriesKey,
        manifestEntries,
      );
      expect(svc.manifestEntriesCache[manifestId]).toEqual(manifestEntries);
    });

    it('reads from storage when present (no API call)', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      await memoryStorage.set(entriesKey, manifestEntries);

      const result = await svc.getManifestEntries({ manifestId });

      expect(apiMocks.manifest).not.toHaveBeenCalled();
      expect(result).toEqual(manifestEntries);
    });
  });

  describe('getById', () => {
    it('returns from storage by checksum and warms in-memory cache', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      await memoryStorage.set(entriesKey, manifestEntries);
      const contentKey = `${cid}.${pid}.${manifestId}:items.sword`;
      await memoryStorage.set(contentKey, {
        [swordEntry.checksum!]: { id: 'items.sword', name: 'Sword' },
      });

      const result = await svc.getById({ manifestId, id: 'items.sword' });

      expect(mockRequester.request).not.toHaveBeenCalled();
      expect(result).toEqual({ id: 'items.sword', name: 'Sword' });
      expect(
        svc.contentsCache[`${manifestId}:items.sword`][swordEntry.checksum!],
      ).toEqual({ id: 'items.sword', name: 'Sword' });
    });

    it('fetches via requester when not in storage and caches it', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      await memoryStorage.set(entriesKey, manifestEntries);
      const contentKey = `${cid}.${pid}.${manifestId}:items.shield`;

      const result = await svc.getById({ manifestId, id: 'items.shield' });

      const expected = {
        id: 'items.shield',
        name: 'Shield',
        tags: ['base'],
        uri: '/content/items/shield.json',
      };

      expect(mockRequester.request).toHaveBeenCalledWith({
        url: shieldEntry.uri,
        withAuth: true,
      });
      expect(result).toEqual(expected);
      expect(await memoryStorage.get(contentKey)).toEqual({
        [shieldEntry.checksum!]: expected,
      });
    });
  });

  it('getByIds resolves multiple contents', async () => {
    const svc = makeService();
    clearStaticCaches(svc);
    await memoryStorage.set(entriesKey, manifestEntries);
    await memoryStorage.set(`${cid}.${pid}.${manifestId}:items.sword`, {
      [swordEntry.checksum!]: { id: 'items.sword', name: 'Sword' },
    });
    await memoryStorage.set(`${cid}.${pid}.${manifestId}:items.shield`, {
      [shieldEntry.checksum!]: { id: 'items.shield', name: 'Shield' },
    });

    const result = await svc.getByIds({
      manifestId,
      ids: ['items.sword', 'items.shield'],
    });

    expect(result).toEqual([
      { id: 'items.sword', name: 'Sword' },
      { id: 'items.shield', name: 'Shield' },
    ]);
  });

  describe('getByType', () => {
    it('filters manifest entries by type prefix and returns contents', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      await memoryStorage.set(entriesKey, manifestEntries);
      await memoryStorage.set(`${cid}.${pid}.${manifestId}:items.sword`, {
        [swordEntry.checksum!]: { id: 'items.sword', name: 'Sword' },
      });
      await memoryStorage.set(`${cid}.${pid}.${manifestId}:items.shield`, {
        [shieldEntry.checksum!]: { id: 'items.shield', name: 'Shield' },
      });

      const result = await svc.getByType({ manifestId, type: 'items' });

      expect(result).toEqual([
        { id: 'items.sword', name: 'Sword' },
        { id: 'items.shield', name: 'Shield' },
      ]);
    });
  });

  describe('refresh', () => {
    it('refreshes manifest and caches checksum + entries', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      apiMocks.manifest.mockResolvedValue({
        status: 200,
        headers: {},
        body: { entries: manifestEntries },
      });

      const data: ContentManifestChecksum = {
        id: manifestId,
        checksum: 'MAN_CHK_2',
        created: '123',
        uid: 'uid-2',
      };

      const res = await svc.refresh(data);
      expect(res).toEqual(data);
      expect(svc.manifestEntriesCache[manifestId]).toEqual(manifestEntries);
      expect(svc.manifestChecksumsCache[manifestId]).toEqual(data);
      expect(memoryStorage.set).toHaveBeenCalledWith(
        entriesKey,
        manifestEntries,
      );
      expect(memoryStorage.set).toHaveBeenCalledWith(checksumKey, data);
    });
  });

  describe('syncContentManifests', () => {
    it('loads from storage when checksum matches and entries exist (no API fetch)', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      // Stored checksum equals latest
      const latest = {
        id: manifestId,
        checksum: 'MAN_CHK',
        createdAt: '1',
        uid: 'uid',
      };
      await memoryStorage.set(checksumKey, {
        id: manifestId,
        checksum: latest.checksum,
        created: latest.createdAt,
        uid: latest.uid,
      });
      await memoryStorage.set(entriesKey, manifestEntries);
      apiMocks.checksum.mockResolvedValue({
        status: 200,
        headers: {},
        body: latest,
      });

      await svc.syncContentManifests({ ids: [manifestId] });

      expect(apiMocks.manifest).not.toHaveBeenCalled();
      expect(svc.manifestEntriesCache[manifestId]).toEqual(manifestEntries);
      expect(svc.manifestChecksumsCache[manifestId]).toEqual({
        id: manifestId,
        checksum: latest.checksum,
        created: latest.createdAt,
        uid: latest.uid,
      });
    });

    it('fetches entries when checksum matches but entries missing in storage', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      const latest = {
        id: manifestId,
        checksum: 'MAN_CHK',
        createdAt: '1',
        uid: 'uid',
      };
      await memoryStorage.set(checksumKey, {
        id: manifestId,
        checksum: latest.checksum,
        created: latest.createdAt,
        uid: latest.uid,
      });
      apiMocks.checksum.mockResolvedValue({
        status: 200,
        headers: {},
        body: latest,
      });
      apiMocks.manifest.mockResolvedValue({
        status: 200,
        headers: {},
        body: { entries: manifestEntries },
      });

      await svc.syncContentManifests({ ids: [manifestId] });

      expect(apiMocks.manifest).toHaveBeenCalled();
      expect(svc.manifestEntriesCache[manifestId]).toEqual(manifestEntries);
      expect(memoryStorage.set).toHaveBeenCalledWith(
        entriesKey,
        manifestEntries,
      );
    });

    it('fetches and caches when checksum differs', async () => {
      const svc = makeService();
      clearStaticCaches(svc);
      const latest = {
        id: manifestId,
        checksum: 'NEW',
        createdAt: '10',
        uid: 'uid-new',
      };
      await memoryStorage.set(checksumKey, {
        id: manifestId,
        checksum: 'OLD',
        created: '0',
        uid: 'uid-old',
      });
      apiMocks.checksum.mockResolvedValue({
        status: 200,
        headers: {},
        body: latest,
      });
      apiMocks.manifest.mockResolvedValue({
        status: 200,
        headers: {},
        body: { entries: manifestEntries },
      });

      await svc.syncContentManifests({ ids: [manifestId] });

      expect(apiMocks.manifest).toHaveBeenCalled();
      expect(svc.manifestChecksumsCache[manifestId]).toEqual({
        id: manifestId,
        checksum: latest.checksum,
        created: latest.createdAt,
        uid: latest.uid,
      });
      expect(svc.manifestEntriesCache[manifestId]).toEqual(manifestEntries);
      expect(memoryStorage.set).toHaveBeenCalledWith(
        entriesKey,
        manifestEntries,
      );
      expect(memoryStorage.set).toHaveBeenCalledWith(checksumKey, {
        id: manifestId,
        checksum: latest.checksum,
        created: latest.createdAt,
        uid: latest.uid,
      });
    });
  });
});
