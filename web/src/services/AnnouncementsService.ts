import type { AnnouncementView } from '@/__generated__/schemas';
import type { RefreshableService } from '@/services/types/RefreshableService';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import {
  announcementsDeleteByObjectId,
  announcementsGetByObjectId,
  announcementsPostClaimByObjectId,
  announcementsPutReadByObjectId,
} from '@/__generated__/apis';

export interface AnnouncementIdParams {
  /**
   * The ID or array of IDs of the announcements to claim, mark as read, or delete.
   * @remarks This can be a single announcement ID or an array of IDs.
   */
  id: string | string[];
}

export class AnnouncementsService
  extends ApiService
  implements RefreshableService<AnnouncementView[]>
{
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'announcements';
  }

  /**
   * Refreshes the announcements for the current player.
   * @remarks This method fetches the latest announcements and updates the player's announcement list.
   * @example
   * ```ts
   * await beam.announcements.refresh();
   * ```
   * @throws {BeamError} If the refresh fails.
   */
  async refresh(): Promise<AnnouncementView[]> {
    return await this.list();
  }

  /**
   * Fetches all active announcements for the current player.
   * @example
   * ```ts
   * // client-side
   * const announcements = await beam.announcements.list();
   * // server-side
   * const announcements = await beamServer.announcements(playerId).list();
   * ```
   * @throws {BeamError} If the request fails.
   */
  async list(): Promise<AnnouncementView[]> {
    const { body } = await announcementsGetByObjectId(
      this.requester,
      this.accountId,
      false,
      this.accountId,
    );

    if (!this.player) return body.announcements;

    this.player.announcements = body.announcements;
    return body.announcements;
  }

  /** Claims one or more announcements for the current player.
   * @remarks This marks the announcements as claimed, allowing the player to access any associated rewards.
   * @example
   * ```ts
   * // client-side:
   * const announcements = beam.announcements;
   * // server-side:
   * const announcements = beamServer.announcements(playerId);
   * // claim a single announcement
   * await announcements.claim({ id: "id" });
   * // claim multiple announcements
   * await announcements.claim({ id: ["id-1", "id-2"] });
   * ```
   * @throws {BeamError} If the announcement ID is invalid or the operation fails.
   */
  async claim(params: AnnouncementIdParams): Promise<void> {
    const announcementIds = params.id;
    const ids = Array.isArray(announcementIds)
      ? announcementIds
      : [announcementIds];
    const idSet = new Set(ids);

    await announcementsPostClaimByObjectId(
      this.requester,
      this.accountId,
      { announcements: ids },
      this.accountId,
    );

    if (!this.player) return;

    this.player.announcements = this.player.announcements.map((a) =>
      idSet.has(a.id) ? { ...a, isClaimed: true } : a,
    );
    // TODO: Handle any rewards associated with the claimed announcements
  }

  /**
   * Marks one or more announcements as read for the current player.
   * @example
   * ```ts
   * // client-side:
   * const announcements = beam.announcements;
   * // server-side:
   * const announcements = beamServer.announcements(playerId);
   * // mark a single announcement as read
   * await announcements.markAsRead({ id: "id" });
   * // mark multiple announcements as read
   * await announcements.markAsRead({ id: ["id-1", "id-2"] });
   * ```
   * @throws {BeamError} If the announcement ID is invalid or the operation fails.
   */
  async markAsRead(params: AnnouncementIdParams): Promise<void> {
    const announcementIds = params.id;
    const ids = Array.isArray(announcementIds)
      ? announcementIds
      : [announcementIds];
    const idSet = new Set(ids);

    await announcementsPutReadByObjectId(
      this.requester,
      this.accountId,
      { announcements: ids },
      this.accountId,
    );

    if (!this.player) return;

    this.player.announcements = this.player.announcements.map((a) =>
      idSet.has(a.id) ? { ...a, isRead: true } : a,
    );
  }

  /**
   * Deletes one or more announcements for the current player.
   * @remarks Ensure any claimable announcements are handled appropriately before deletion.
   * @example
   * ```ts
   * // client-side:
   * const announcements = beam.announcements;
   * // server-side:
   * const announcements = beamServer.announcements(playerId);
   * // delete a single announcement
   * await announcements.delete({ id: "id" });
   * // delete multiple announcements
   * await announcements.delete({ id: ["id-1", "id-2"] });
   * ```
   * @throws {BeamError} If the announcement ID is invalid or the operation fails.
   */
  async delete(params: AnnouncementIdParams): Promise<void> {
    const announcementIds = params.id;
    const ids = Array.isArray(announcementIds)
      ? announcementIds
      : [announcementIds];
    const idSet = new Set(ids);

    await announcementsDeleteByObjectId(
      this.requester,
      this.accountId,
      { announcements: ids },
      this.accountId,
    );

    if (!this.player) return;

    this.player.announcements = this.player.announcements.filter(
      (a) => !idSet.has(a.id),
    );
  }
}
