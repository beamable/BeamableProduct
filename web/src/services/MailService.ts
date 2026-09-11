import type { Message } from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { FUNNEL_CATEGORY } from '@/services/AnalyticsService';
import {
  mailGetDetailByObjectId,
  mailPostSearchByObjectId,
  mailPutBulkByObjectId,
} from '@/__generated__/apis';

/** Mail states the platform recognises. */
export const MailState = {
  Unread: 'Unread',
  Read: 'Read',
  Claimed: 'Claimed',
  Deleted: 'Deleted',
} as const;

export type MailStateValue = (typeof MailState)[keyof typeof MailState];

export interface MailListParams {
  /** Restrict to these categories. Omit for every category. */
  categories?: string[];
  /** Restrict to these states. Defaults to unread and read, i.e. everything not deleted. */
  states?: MailStateValue[];
  /** Maximum number of messages. */
  limit?: number;
  /** Page from this message id onwards. */
  start?: bigint | string;
}

export interface MailUpdateParams {
  /** The message id, or ids, to update. */
  id: bigint | string | (bigint | string)[];
  /** The state to move them to. */
  state: MailStateValue;
  /** Whether to accept any attachments as part of the same update. */
  acceptAttachments?: boolean;
}

/** Wire keys a campaign message rail stamps onto a mail's metadata. */
const OUTREACH_KEY = 'beam_outreach';
const TRACK_ID_KEY = 'trackId';

/** How many attributed mails to remember. Bounds a long session that pages through a big mailbox. */
const MAX_REMEMBERED = 256;

/**
 * The player's in-game mailbox.
 *
 * Beyond wrapping the mail endpoints, this reports the campaign funnel's `Opened` stage
 * **automatically** when a mail sent by a campaign moves from Unread to Read. Games write no
 * analytics code: reading mail through this service is enough.
 *
 * Why it has to live here rather than in the game: for push, the native SDKs already report `Opened`
 * from inside the notification tap handler, so a game never asks for it. In-game mail has no handset
 * to echo anything back, so this service IS the equivalent interception point — and putting the
 * burden on each game would mean every game that forgot silently reported nothing.
 *
 * @example
 * ```ts
 * const mail = await beam.mail.list({ limit: 20 });
 * await beam.mail.markAsRead({ id: mail[0].id });   // funnel Opened is reported for you
 * ```
 */
export class MailService extends ApiService {
  /**
   * Attribution for mail this session has seen, keyed by message id, so a state change can be
   * reported without the caller handing the message back.
   */
  private readonly attributed = new Map<string, { outreachId: string; trackId: string }>();

  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'mail';
  }

  /**
   * Lists the player's mail.
   * @throws {BeamError} If the request fails.
   */
  async list(params: MailListParams = {}): Promise<Message[]> {
    const { body } = await mailPostSearchByObjectId(
      this.requester,
      this.accountId,
      {
        clauses: [
          {
            name: 'inbox',
            onlyCount: false,
            ...(params.categories ? { categories: params.categories } : {}),
            states: params.states ?? [MailState.Unread, MailState.Read],
            ...(params.limit != null ? { limit: params.limit } : {}),
            ...(params.start != null ? { start: params.start } : {}),
          },
        ],
      },
      this.accountId,
    );

    const messages = (body?.results ?? []).flatMap((clause) => clause.content ?? []);
    this.remember(messages);
    return messages;
  }

  /**
   * Fetches a single message.
   * @throws {BeamError} If the request fails.
   */
  async get(id: bigint | string): Promise<Message | undefined> {
    const { body } = await mailGetDetailByObjectId(
      this.requester,
      this.accountId,
      id,
      this.accountId,
    );

    const message = body?.result;
    if (message) this.remember([message]);
    return message;
  }

  /**
   * Marks one or more messages as read, reporting the campaign funnel's `Opened` for any that came
   * from a campaign.
   * @throws {BeamError} If the request fails.
   */
  async markAsRead(id: MailUpdateParams['id']): Promise<void> {
    await this.update({ id, state: MailState.Read });
  }

  /**
   * Moves one or more messages to a new state.
   * @remarks Reports the campaign funnel's `Opened` on an Unread -> Read transition. Reported on the
   * state change rather than on display, so the client's count and the server's own transition agree.
   * @throws {BeamError} If the request fails.
   */
  async update(params: MailUpdateParams): Promise<void> {
    const ids = Array.isArray(params.id) ? params.id : [params.id];
    if (ids.length === 0) return;

    await mailPutBulkByObjectId(
      this.requester,
      this.accountId,
      {
        updateMailRequests: ids.map((mailId) => ({
          id: mailId,
          update: {
            mailId,
            state: params.state,
            ...(params.acceptAttachments != null
              ? { acceptAttachments: params.acceptAttachments }
              : {}),
          },
        })),
      },
      this.accountId,
    );

    if (params.state === MailState.Read) this.reportOpened(ids);
  }

  /** Records attribution for any campaign mail in this batch. Ordinary game mail carries none. */
  private remember(messages: Message[]): void {
    for (const message of messages) {
      const metadata = (message as Message & { metadata?: Record<string, string> }).metadata;
      const outreachId = metadata?.[OUTREACH_KEY];
      const trackId = metadata?.[TRACK_ID_KEY];

      // Both are required downstream: outreachId is the funnel's per-recipient dedup key, trackId is
      // what the platform parses to find the campaign and node. One without the other is unusable.
      if (!outreachId || !trackId) continue;

      if (this.attributed.size >= MAX_REMEMBERED) {
        // Insertion-order eviction. An exact LRU is not worth a second data structure for what is a
        // best-effort metric on a bounded mailbox page.
        const oldest = this.attributed.keys().next();
        if (!oldest.done) this.attributed.delete(oldest.value);
      }

      this.attributed.set(String(message.id), { outreachId, trackId });
    }
  }

  /**
   * Emits one `Opened` per campaign mail in the batch.
   *
   * Deliberately not awaited and never throws: a failed metrics call must not make a mail look
   * unread. The platform also dedupes on (outreachId, stage), so a re-read cannot double count —
   * dropping it from the map here just avoids sending the request twice.
   */
  private reportOpened(ids: (bigint | string)[]): void {
    if (this.attributed.size === 0) return;

    const analytics = this.analytics();
    if (!analytics) return;

    for (const id of ids) {
      const key = String(id);
      const attribution = this.attributed.get(key);
      if (!attribution) continue;
      this.attributed.delete(key);

      // Byte-identical in shape to what the push SDKs already send, so the platform's campaign
      // consumer attributes it with no ingest change: it keys on category + trackId + outreachId.
      analytics.trackSafely({
        name: 'Opened',
        category: FUNNEL_CATEGORY,
        params: {
          outreachId: attribution.outreachId,
          trackId: attribution.trackId,
          funnelType: 'ingame',
          mailId: key,
        },
      });
    }
  }

  /**
   * The analytics service, or undefined when the game did not register it.
   *
   * Reading `beam.analytics` is not a plain property access: unregistered services are backed by a
   * getter that THROWS (`BeamBase.throwServiceUnavailable`). A truthiness check alone is therefore
   * not enough - without the catch, a game that registered mail but not analytics would have
   * `markAsRead` reject *after* the mail was already updated on the server, which looks exactly like
   * a failed read.
   */
  private analytics(): { trackSafely: (event: unknown) => void } | undefined {
    try {
      return (this.beam as { analytics?: { trackSafely: (event: unknown) => void } }).analytics;
    } catch {
      return undefined;
    }
  }
}
