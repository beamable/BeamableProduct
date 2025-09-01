import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import {
  contentGetManifestChecksumBasic,
  contentGetManifestPublicJsonBasic,
} from '@/__generated__/apis';
import { defaultContentStorage } from '@/defaults';
import { ClientContentInfoJson } from '@/__generated__/schemas';
import type { RefreshableService } from '@/services/types';
import { ContentStorage } from '@/platform/types/ContentStorage';
import type {
  ContentBase,
  ContentTypeFromId,
  ContentTypeMap,
} from '@/contents/types';
import { BeamError } from '@/constants/Errors';

interface CachedContentData {
  [key: string]: ContentBase | undefined;
}

export interface ContentManifestChecksum {
  id: string;
  checksum: string;
  created: bigint | string;
  uid?: string;
}

export interface GetManifestEntriesParams {
  /** Optional manifest ID to fetch specific content, defaults to 'global' */
  manifestId?: string;
}

export interface GetContentByIdParams<T extends string = string> {
  /** Optional manifest ID to fetch specific content, defaults to 'global' */
  manifestId?: string;
  id: T;
}

export interface GetContentByIdsParams {
  /** Optional manifest ID to fetch specific content, defaults to 'global' */
  manifestId?: string;
  ids: string[];
}

export interface GetContentByTypeParams<T extends keyof ContentTypeMap> {
  /** Optional manifest ID to fetch specific content, defaults to 'global' */
  manifestId?: string;
  type: T;
}

/** @internal */
export interface GetContentManifestsParams {
  ids?: string[];
}

export class ContentService
  extends ApiService
  implements RefreshableService<ContentManifestChecksum>
{
  private static _contentsCache: Record<string, Record<string, ContentBase>> =
    {};
  private static _manifestChecksumsCache: Record<
    string,
    ContentManifestChecksum
  > = {};
  private static _manifestEntriesCache: Record<
    string,
    ClientContentInfoJson[]
  > = {};

  private readonly contentStoragePromise: Promise<ContentStorage>;

  constructor(props: ApiServiceProps) {
    super(props);
    this.contentStoragePromise = defaultContentStorage();
  }

  /** @internal */
  get serviceName(): string {
    return 'content';
  }

  /** Retrieves the contents cache. */
  get contentsCache(): Record<string, Record<string, ContentBase>> {
    return ContentService._contentsCache;
  }

  /** Retrieves the manifest checksums cache. */
  get manifestChecksumsCache(): Record<string, ContentManifestChecksum> {
    return ContentService._manifestChecksumsCache;
  }

  /** Retrieves the manifest entries cache. */
  get manifestEntriesCache(): Record<string, ClientContentInfoJson[]> {
    return ContentService._manifestEntriesCache;
  }

  /**
   * Refreshes the content manifest for a given ID.
   * @remarks This method fetches the latest content manifest and updates the local cache.
   * @example
   * ```ts
   * await beam.content.refresh({
   *   id: 'global',
   *   checksum: 'some-checksum',
   *   uid: 'some-uid',
   * });
   * ```
   * @throws {BeamError} If the refresh fails.
   */
  async refresh(
    data: ContentManifestChecksum,
  ): Promise<ContentManifestChecksum> {
    await this.fetchAndCacheManifestEntries(data.id, data);
    return {
      id: data.id,
      checksum: data.checksum,
      created: data.created,
      uid: data.uid,
    };
  }

  /**
   * Retrieves all manifest entries for a given manifest ID.
   * @remarks This method first checks the in-memory cache, then persistent storage, and finally fetches from the API if not found.
   * @example
   * ```ts
   * const entries = await beam.content.getManifestEntries({
   *   manifestId: 'global', // Optional, defaults to 'global'
   * });
   * console.log(entries[0].contentId);
   * ```
   * @returns An array of manifest entries.
   */
  async getManifestEntries(
    params: GetManifestEntriesParams = {},
  ): Promise<ClientContentInfoJson[]> {
    const { manifestId = 'global' } = params;
    const manifestEntriesKey = this.getManifestEntriesKey(manifestId);
    const contentStorage = await this.contentStoragePromise;
    return (
      ContentService._manifestEntriesCache[manifestId] ??
      (await contentStorage.get<ClientContentInfoJson[]>(manifestEntriesKey)) ??
      (await this.fetchAndCacheManifestEntries(manifestId))
    );
  }

  /**
   * Retrieves content by its ID.
   * @remarks This method first checks the in-memory cache, then persistent storage, and finally fetches from the API if not found or if the content is outdated.
   * @example
   * ```ts
   * const item = await beam.content.getById({
   *   id: 'items.my_item',
   *   manifestId: 'global', // Optional, defaults to 'global'
   * });
   * console.log(item.properties);
   * ```
   * @returns The content object.
   * @throws {BeamError} If the content is not found or cannot be retrieved.
   */
  async getById<T extends string>(
    params: GetContentByIdParams<T>,
  ): Promise<ContentTypeFromId<T>> {
    const { id, manifestId = 'global' } = params;
    // Get the content's manifest entry (from cache or storage or API).
    const contentEntry = await this.getContentEntryFromManifest(manifestId, id);
    return this.getContentDataByEntry(manifestId, contentEntry);
  }

  /**
   * Retrieves group of contents by their IDs.
   * @remarks This method first checks the in-memory cache, then persistent storage, and finally fetches from the API if not found or if the content is outdated.
   * @example
   * ```ts
   * const items = await beam.content.getByIds({
   *   ids: ['items.my_item_1', 'items.my_item_2'],
   *   manifestId: 'global', // Optional, defaults to 'global'
   * });
   * console.log(items[0].properties);
   * ```
   * @returns An array of content objects.
   * @throws {BeamError} If any content is not found or cannot be retrieved.
   */
  async getByIds(params: GetContentByIdsParams): Promise<ContentBase[]> {
    const { ids, manifestId = 'global' } = params;
    const contentEntries = await Promise.all(
      ids.map((id) => this.getContentEntryFromManifest(manifestId, id)),
    );
    return Promise.all(
      contentEntries.map((entry) =>
        this.getContentDataByEntry(manifestId, entry),
      ),
    );
  }

  /**
   * Retrieves group of contents by their type.
   * @remarks This method first checks the in-memory cache, then persistent storage, and finally fetches from the API if not found or if the content is outdated.
   * @example
   * ```ts
   * const items = await beam.content.getByType({
   *   type: 'items',
   *   manifestId: 'global', // Optional, defaults to 'global'
   * });
   * console.log(items[0].properties);
   * ```
   * @returns An array of content objects of the specified type.
   * @throws {BeamError} If the content is not found or cannot be retrieved.
   */
  async getByType<T extends keyof ContentTypeMap>(
    params: GetContentByTypeParams<T>,
  ): Promise<ContentTypeFromId<T>[]> {
    const { type, manifestId = 'global' } = params;
    // Get the contents' manifest entry (from cache or storage or API).
    const contentEntries = await this.getContentEntriesFromManifest(
      manifestId,
      type,
    );
    return Promise.all(
      contentEntries.map((entry) =>
        this.getContentDataByEntry<T>(manifestId, entry),
      ),
    );
  }

  /** Retrieves a content entry from the manifest, fetching from the API if not cached. */
  private async getContentEntryFromManifest(
    manifestId: string,
    contentId: string,
  ): Promise<ClientContentInfoJson> {
    const manifestEntriesKey = this.getManifestEntriesKey(manifestId);
    const contentStorage = await this.contentStoragePromise;
    // Check in-memory cache first, then storage
    const cachedManifestEntries =
      ContentService._manifestEntriesCache[manifestId] ??
      (await contentStorage.get<ClientContentInfoJson[]>(manifestEntriesKey)) ??
      (await this.fetchAndCacheManifestEntries(manifestId));

    let contentEntry = cachedManifestEntries.find(
      (entry) => entry.contentId === contentId,
    );

    if (contentEntry) {
      return contentEntry;
    }

    // If not in cache or storage, fetch, cache, and then find the entry
    const manifestEntries = await this.fetchAndCacheManifestEntries(manifestId);

    contentEntry = manifestEntries.find(
      (entry) => entry.contentId === contentId,
    );

    if (!contentEntry) {
      throw new BeamError(
        `Content with ID ${contentId} not found in manifest ${manifestId}.`,
      );
    }

    return contentEntry;
  }

  /** Retrieves content entries from the manifest, fetching from the API if not cached. */
  private async getContentEntriesFromManifest(
    manifestId: string,
    contentType: keyof ContentTypeMap,
  ) {
    const manifestEntriesKey = this.getManifestEntriesKey(manifestId);
    const contentStorage = await this.contentStoragePromise;
    // Check in-memory cache first, then storage
    const manifestEntries =
      ContentService._manifestEntriesCache[manifestId] ??
      (await contentStorage.get<ClientContentInfoJson[]>(manifestEntriesKey)) ??
      [];

    return manifestEntries.filter((entry) =>
      entry.contentId.startsWith(contentType),
    );
  }

  /** Retrieves content data from a manifest entry, using cache or fetching as needed. */
  private async getContentDataByEntry<T extends string>(
    manifestId: string,
    contentEntry: ClientContentInfoJson,
  ): Promise<ContentTypeFromId<T>> {
    if (!contentEntry.uri) {
      throw new BeamError(
        `Content entry for ID ${contentEntry.contentId} does not have a valid URI.`,
      );
    }

    const contentEntryChecksum = contentEntry.checksum;
    // Check in-memory cache first for immediate access
    const inMemoryKey = `${manifestId}:${contentEntry.contentId}`;
    if (
      contentEntryChecksum &&
      ContentService._contentsCache[inMemoryKey]?.[contentEntryChecksum]
    ) {
      return ContentService._contentsCache[inMemoryKey][
        contentEntryChecksum
      ] as ContentTypeFromId<T>;
    }

    // Check the persistent storage for content matching the checksum.
    const contentKey = this.getContentKey(inMemoryKey);
    const contentStorage = await this.contentStoragePromise;

    const cachedContentData =
      (await contentStorage.get<CachedContentData>(contentKey)) ?? {};

    if (contentEntryChecksum && cachedContentData[contentEntryChecksum]) {
      const contentData = cachedContentData[contentEntryChecksum];
      // Cache hit. Populate the in-memory cache and return the content.
      ContentService._contentsCache[inMemoryKey] = {
        [contentEntryChecksum]: contentData,
      };
      return contentData as ContentTypeFromId<T>;
    }

    // Cache miss. Fetch from URI, cache the result, and return it.
    return this.fetchAndCacheContentData(
      manifestId,
      contentEntry.contentId as T,
      contentEntry,
      contentEntryChecksum,
    );
  }

  /** Fetches content data from a URI, validates it, and caches it. */
  private async fetchAndCacheContentData<T extends string>(
    manifestId: string,
    id: T,
    contentEntry: ClientContentInfoJson,
    contentChecksum: string | undefined,
  ): Promise<ContentTypeFromId<T>> {
    const contentKey = this.getContentKey(`${manifestId}:${id}`);
    const contentStorage = await this.contentStoragePromise;

    // Fetch content from its URI
    const { body: contentData } = await this.requester.request<
      ContentTypeFromId<T>
    >({
      url: contentEntry.uri,
      withAuth: true,
    });

    if (!contentData) {
      throw new BeamError(
        `Failed to fetch content with ID ${id} from manifest ${manifestId}.`,
      );
    }

    contentData['uri'] = contentEntry.uri;
    contentData['tags'] = contentEntry.tags;
    contentData['createdAt'] = contentEntry.createdAt;

    // Cache the fetched content
    const contentDataWithChecksum = {
      [contentChecksum ?? 'no-checksum']: contentData,
    };
    ContentService._contentsCache[`${manifestId}:${id}`] =
      contentDataWithChecksum;
    await contentStorage.set<CachedContentData>(
      contentKey,
      contentDataWithChecksum,
    );

    return contentData as ContentTypeFromId<T>;
  }

  /**
   * @internal
   * Synchronizes content manifests between local storage and the server.
   *
   * For each provided manifest ID:
   * - Retrieves the locally cached manifest checksums and entries.
   * - Fetches the latest manifest checksum from the API.
   * - If the checksums differ or no local checksum exists:
   *   - Fetches the full manifest from the API.
   *   - Caches the manifest entries and updated checksum locally.
   *
   * @param params Optional parameters for specifying manifest IDs to sync.
   *               Defaults to ['global'] if not provided.
   */
  async syncContentManifests(
    params: GetContentManifestsParams = {},
  ): Promise<void> {
    const { ids = ['global'] } = params;
    // Create a sync task for each manifest ID
    const syncTasks = ids.map((id) => this.syncSingleManifest(id));
    // Run all sync tasks in parallel.
    await Promise.all(syncTasks);
  }

  /** Syncs a single content manifest by comparing its local and remote checksums. */
  private async syncSingleManifest(manifestId: string): Promise<void> {
    const contentStorage = await this.contentStoragePromise;
    const checksumKey = this.getManifestChecksumKey(manifestId);

    // Fetch the locally stored checksum and the latest checksum from the API in parallel.
    const [cachedChecksum, { body: latestChecksum }] = await Promise.all([
      contentStorage.get<ContentManifestChecksum>(checksumKey),
      contentGetManifestChecksumBasic(
        this.requester,
        manifestId,
        undefined,
        this.accountId,
      ),
    ]);

    // Compare the checksums to see if an update is needed.
    if (cachedChecksum?.checksum === latestChecksum.checksum) {
      // Checksums match. The local version is up-to-date.
      const entriesKey = this.getManifestEntriesKey(manifestId);
      const manifestEntries =
        await contentStorage.get<ClientContentInfoJson[]>(entriesKey);

      if (!manifestEntries) {
        // Manifest entries not found in storage. Fetch them from the API.
        await this.fetchAndCacheManifestEntries(manifestId, {
          id: latestChecksum.id,
          checksum: latestChecksum.checksum,
          created: latestChecksum.createdAt,
          uid: latestChecksum.uid,
        });
        return;
      }

      // Manifest entries found in storage. Load them into the in-memory cache.
      ContentService._manifestChecksumsCache[manifestId] = cachedChecksum;
      ContentService._manifestEntriesCache[manifestId] = manifestEntries;
    } else {
      // Checksums differ. Fetch the manifest entries from the API and cache it.
      await this.fetchAndCacheManifestEntries(manifestId, {
        id: latestChecksum.id,
        checksum: latestChecksum.checksum,
        created: latestChecksum.createdAt,
        uid: latestChecksum.uid,
      });
    }
  }

  /** Fetches a content manifest entries from the API and caches it locally. */
  private async fetchAndCacheManifestEntries(
    manifestId: string,
    manifestChecksum?: ContentManifestChecksum,
  ): Promise<ClientContentInfoJson[]> {
    const contentStorage = await this.contentStoragePromise;
    const { body } = await contentGetManifestPublicJsonBasic(
      this.requester,
      manifestId,
      undefined,
      this.accountId,
    );
    const manifestEntries = body.entries ?? [];

    // in-memory cache
    ContentService._manifestEntriesCache[manifestId] = manifestEntries;
    if (manifestChecksum) {
      ContentService._manifestChecksumsCache[manifestId] = manifestChecksum;
    }

    // persist to storage
    const promises = [];
    promises.push(
      contentStorage.set<ClientContentInfoJson[]>(
        this.getManifestEntriesKey(manifestId),
        body.entries,
      ),
    );
    if (manifestChecksum) {
      promises.push(
        contentStorage.set<ContentManifestChecksum>(
          this.getManifestChecksumKey(manifestId),
          manifestChecksum,
        ),
      );
    }

    await Promise.all(promises);
    return manifestEntries;
  }

  private getManifestChecksumKey(manifestId: string) {
    const { cid, pid } = this.beam;
    return `${cid}.${pid}.${manifestId}.checksum`;
  }

  private getManifestEntriesKey(manifestId: string) {
    const { cid, pid } = this.beam;
    return `${cid}.${pid}.${manifestId}.entries`;
  }

  private getContentKey(contentId: string) {
    const { cid, pid } = this.beam;
    return `${cid}.${pid}.${contentId}`;
  }
}
