import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';

/**
 * One analytics event, in the platform's compact wire shape.
 *
 * The field names are single letters because that is the contract the platform parses, not a size
 * optimisation on our side: `ClientAnalyticsEvent` on the server binds `e`/`p`/`time`.
 */
export interface AnalyticsEvent {
  /** The event name. This is the field a campaign conversion goal matches on. */
  name: string;
  /**
   * Event parameters. Nested objects are flattened server-side to dot-noted keys (`details.price`),
   * which is what a goal's value conditions are evaluated against.
   */
  params?: Record<string, unknown>;
  /** Event time in epoch milliseconds. Defaults to the server's receive time when omitted. */
  time?: number;
}

/**
 * The campaign funnel stages a client may report, spelled as they go on the wire.
 *
 * Every name the platform itself emits lives under the reserved `beam_` prefix, so a game's own event
 * can never be mistaken for a funnel stage: the platform refuses to publish a campaign that watches a
 * `beam_` name, and a stage arriving under one is therefore proof of its producer. A cross-platform
 * contract — iOS `FunnelType`, Android `FunnelType` and Unity send exactly these strings. The old
 * unprefixed names (`Opened`, `Received`, ...) are no longer watched and count nothing.
 */
export const FunnelStage = {
  Delivered: 'beam_delivered',
  Opened: 'beam_opened',
  Clicked: 'beam_clicked',
} as const;

/**
 * Sends analytics events to the platform.
 *
 * Until now the web SDK could only QUERY analytics (`analyticsPostQuery`) — it had no way to emit at
 * all, which is why a web or React Native game could not report campaign funnel stages the way Unity
 * and the native push SDKs do. This closes that gap.
 *
 * @example
 * ```ts
 * await beam.analytics.track({ name: 'level_complete', params: { level: 7 } });
 * ```
 */
export class AnalyticsService extends ApiService {
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'analytics';
  }

  /**
   * Sends one analytics event.
   * @throws {BeamError} If the request fails.
   */
  async track(event: AnalyticsEvent): Promise<void> {
    await this.trackBatch([event]);
  }

  /**
   * Sends several analytics events in one request.
   * @remarks The platform accepts a batch and fans it out, so prefer this over repeated `track`
   * calls when reporting more than one event at a time.
   * @throws {BeamError} If the request fails.
   */
  async trackBatch(events: AnalyticsEvent[]): Promise<void> {
    if (events.length === 0) return;

    await makeApiRequest<void, ReturnType<typeof toWire>[]>({
      r: this.requester,
      e: '/analytics/events',
      m: POST,
      p: events.map(toWire),
      w: true,
    });
  }

  /**
   * Fire-and-forget variant: never throws and never blocks the caller.
   *
   * Analytics must not be able to fail the thing that produced them — a mail that will not open
   * because a metrics call 500'd is a far worse outcome than a missing data point. Used by the SDK's
   * own implicit reporting.
   */
  trackSafely(event: AnalyticsEvent): void {
    void this.track(event).catch(() => {
      /* deliberately swallowed - see above */
    });
  }
}

function toWire(event: AnalyticsEvent) {
  return {
    e: event.name,
    p: event.params ?? {},
    ...(event.time ? { time: event.time } : {}),
  };
}
