import { BeamApi } from '@/core/BeamApi';
import { AnnouncementView } from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';
import { Refreshable } from '@/services/types/Refreshable';

interface AnnouncementsServiceProps {
  api: BeamApi;
  player: PlayerService;
}

export interface AnnouncementIdParams {
  id: string | string[];
}

export class AnnouncementsService implements Refreshable<AnnouncementView[]> {
  private readonly api: BeamApi;
  private readonly player: PlayerService;

  /** @internal */
  constructor(props: AnnouncementsServiceProps) {
    this.api = props.api;
    this.player = props.player;
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
   * const announcements = await beam.announcements.list();
   * ```
   * @throws {BeamError} If the request fails.
   */
  async list(): Promise<AnnouncementView[]> {
    const { body } = await this.api.announcements.getAnnouncementByObjectId(
      this.player.id,
      false,
    );
    this.player.announcements = body.announcements;
    return body.announcements;
  }

  /** Claims one or more announcements for the current player.
   * @remarks This marks the announcements as claimed, allowing the player to access any associated rewards.
   * @example
   * ```ts
   * await beam.announcements.claim({ id: "id" });
   * // or to claim multiple announcements
   * await beam.announcements.claim({ id: ["id-1", "id-2"] });
   * ```
   * @throws {BeamError} If the announcement ID is invalid or the operation fails.
   */
  async claim(params: AnnouncementIdParams): Promise<void> {
    const announcementIds = params.id;
    const ids = Array.isArray(announcementIds)
      ? announcementIds
      : [announcementIds];
    const idSet = new Set(ids);

    await this.api.announcements.postAnnouncementClaimByObjectId(
      this.player.id,
      {
        announcements: ids,
      },
    );

    this.player.announcements = this.player.announcements.map((a) =>
      idSet.has(a.id) ? { ...a, isClaimed: true } : a,
    );
    // TODO: Handle any rewards associated with the claimed announcements
  }

  /**
   * Marks one or more announcements as read for the current player.
   * @example
   * ```ts
   * await beam.announcements.markAsRead({ id: "id" });
   * // or to mark multiple announcements as read
   * await beam.announcements.markAsRead({ id: ["id-1", "id-2"] });
   * ```
   * @throws {BeamError} If the announcement ID is invalid or the operation fails.
   */
  async markAsRead(params: AnnouncementIdParams): Promise<void> {
    const announcementIds = params.id;
    const ids = Array.isArray(announcementIds)
      ? announcementIds
      : [announcementIds];
    const idSet = new Set(ids);

    await this.api.announcements.putAnnouncementReadByObjectId(this.player.id, {
      announcements: ids,
    });

    this.player.announcements = this.player.announcements.map((a) =>
      idSet.has(a.id) ? { ...a, isRead: true } : a,
    );
  }

  /**
   * Deletes one or more announcements for the current player.
   * @remarks Ensure any claimable announcements are handled appropriately before deletion.
   * @example
   * ```ts
   * await beam.announcements.delete({ id: "id" });
   * // or to delete multiple announcements
   * await beam.announcements.delete({ id: ["id-1", "id-2"] });
   * ```
   * @throws {BeamError} If the announcement ID is invalid or the operation fails.
   */
  async delete(params: AnnouncementIdParams): Promise<void> {
    const announcementIds = params.id;
    const ids = Array.isArray(announcementIds)
      ? announcementIds
      : [announcementIds];
    const idSet = new Set(ids);

    await this.api.announcements.deleteAnnouncementByObjectId(this.player.id, {
      announcements: ids,
    });

    this.player.announcements = this.player.announcements.filter(
      (a) => !idSet.has(a.id),
    );
  }
}
