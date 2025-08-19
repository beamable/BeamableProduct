import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
  type Mock,
} from 'vitest';
import { ContentService } from '@/services/ContentService';
import * as apis from '@/__generated__/apis';
import * as defaults from '@/defaults';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { BeamBase } from '@/core/BeamBase';
import type { ContentStorage } from '@/platform/types/ContentStorage';
import { BeamError } from '@/constants/Errors';
import { ContentType } from '@/__generated__/schemas';

describe('ContentService', () => {
  let contentService: ContentService;
  let mockRequester: HttpRequester;
  let mockBeam: BeamBase;
  let mockStorage: ContentStorage;

  beforeEach(() => {
    mockRequester = {
      request: vi.fn(),
    } as unknown as HttpRequester;

    mockBeam = {
      cid: 'test-cid',
      pid: 'test-pid',
      requester: mockRequester,
      accountId: 'test-account-id',
    } as unknown as BeamBase;

    mockStorage = {
      get: vi.fn(),
      set: vi.fn(),
    } as unknown as ContentStorage;

    vi.spyOn(defaults, 'defaultContentStorage').mockResolvedValue(mockStorage);

    ContentService['contentCache'] = {};
    ContentService['manifestChecksumsCache'] = {};
    ContentService['manifestEntriesCache'] = {};

    contentService = new ContentService({ beam: mockBeam });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getById', () => {
    it('should retrieve content from the API when not in cache or storage', async () => {
      const manifestId = 'global';
      const contentId = 'items.my_item';
      const checksum = 'test-checksum';
      const uri = 'test-uri';
      const contentData = { properties: { foo: 'bar' } };

      vi.spyOn(apis, 'contentGetManifestChecksumBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { id: manifestId, checksum, createdAt: '0' },
      });

      vi.spyOn(apis, 'contentGetManifestPublicJsonBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {
          entries: [
            {
              contentId,
              checksum,
              uri,
              tags: [],
              version: '',
              type: ContentType.Content,
            },
          ],
        },
      });

      vi.spyOn(mockRequester, 'request').mockResolvedValue({
        status: 200,
        headers: {},
        body: contentData,
      });

      const result = await contentService.getById({ id: contentId });

      expect(result).toEqual(contentData);
      expect(mockStorage.get).toHaveBeenCalledTimes(2);
      expect(apis.contentGetManifestPublicJsonBasic).toHaveBeenCalledTimes(1);
      expect(mockRequester.request).toHaveBeenCalledWith({
        url: uri,
        withAuth: true,
      });
      expect(mockStorage.set).toHaveBeenCalledTimes(2);
    });

    it('should retrieve content from storage if available', async () => {
      const contentId = 'items.my_item';
      const checksum = 'test-checksum';
      const uri = 'test-uri';
      const contentData = { properties: { foo: 'bar' } };

      vi.spyOn(apis, 'contentGetManifestPublicJsonBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {
          entries: [],
        },
      });

      // Setup manifest in storage
      (mockStorage.get as Mock).mockImplementation((key: string) => {
        if (key.endsWith('.entries')) {
          return Promise.resolve([
            {
              contentId,
              checksum,
              uri,
              tags: [],
              version: '',
              visibility: '',
              lastModified: 0,
            },
          ]);
        }
        if (key.endsWith(contentId)) {
          return Promise.resolve({ [checksum]: contentData });
        }
        return Promise.resolve(undefined);
      });

      const result = await contentService.getById({ id: contentId });

      expect(result).toEqual(contentData);
      expect(mockStorage.get).toHaveBeenCalledTimes(2);
      expect(apis.contentGetManifestPublicJsonBasic).not.toHaveBeenCalled();
      expect(mockRequester.request).not.toHaveBeenCalled();
    });

    it('should throw a BeamError if content is not found in the manifest', async () => {
      const manifestId = 'global';
      const contentId = 'items.my_item';
      const checksum = 'test-checksum';

      vi.spyOn(apis, 'contentGetManifestChecksumBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { id: manifestId, checksum, createdAt: '0' },
      });

      vi.spyOn(apis, 'contentGetManifestPublicJsonBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {
          entries: [],
        },
      });

      await expect(contentService.getById({ id: contentId })).rejects.toThrow(
        new BeamError(
          `Content with ID ${contentId} not found in manifest ${manifestId}.`,
        ),
      );
    });
  });

  describe('getByType', () => {
    it('should retrieve all content of a specific type', async () => {
      const contentType = 'items';
      const checksum1 = 'test-checksum-1';
      const checksum2 = 'test-checksum-2';
      const uri1 = 'test-uri-1';
      const uri2 = 'test-uri-2';
      const contentData1 = { properties: { foo: 'bar' } };
      const contentData2 = { properties: { baz: 'qux' } };

      vi.spyOn(apis, 'contentGetManifestPublicJsonBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {
          entries: [],
        },
      });

      // Setup manifest in storage
      (mockStorage.get as Mock).mockImplementation((key: string) => {
        if (key.endsWith('.entries')) {
          return Promise.resolve([
            {
              contentId: 'items.my_item_1',
              checksum: checksum1,
              uri: uri1,
              tags: [],
              version: '',
              visibility: '',
              lastModified: 0,
            },
            {
              contentId: 'items.my_item_2',
              checksum: checksum2,
              uri: uri2,
              tags: [],
              version: '',
              visibility: '',
              lastModified: 0,
            },
            {
              contentId: 'other.my_other_item',
              checksum: 'other-checksum',
              uri: 'other-uri',
              tags: [],
              version: '',
              visibility: '',
              lastModified: 0,
            },
          ]);
        }
        if (key.endsWith('items.my_item_1')) {
          return Promise.resolve({ [checksum1]: contentData1 });
        }
        if (key.endsWith('items.my_item_2')) {
          return Promise.resolve({ [checksum2]: contentData2 });
        }
        return Promise.resolve(undefined);
      });

      const result = await contentService.getByType({ type: contentType });

      expect(result).toEqual([contentData1, contentData2]);
      expect(mockStorage.get).toHaveBeenCalledTimes(3);
      expect(apis.contentGetManifestPublicJsonBasic).not.toHaveBeenCalled();
      expect(mockRequester.request).not.toHaveBeenCalled();
    });
  });
});
