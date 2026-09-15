/**
 * Realtime notifications the player socket delivers as-is.
 *
 * These are distinct from `RefreshableServiceMap` contexts. A refreshable context means "this
 * service's data changed, here is the re-fetched result" — the SDK calls the service's `refresh()`
 * and hands you what it returned. A notification context means "this happened" — the payload on the
 * wire *is* the data, and nothing is re-fetched on your behalf.
 *
 * Both are subscribed the same way, through `beam.on(context, handler)`.
 */
export interface NotificationEventMap {
  'segments.transition': SegmentMembershipChanged;
}

/**
 * @internal
 * The notification contexts `beam.on` accepts, as a runtime value.
 *
 * `NotificationEventMap` is erased at build time, so the subscription guard needs this to tell an
 * unsupported context from a supported one. Keep it in step with the interface above — a context in
 * the map but missing here is rejected at runtime despite type-checking.
 */
export const NOTIFICATION_CONTEXTS: (keyof NotificationEventMap)[] = [
  'segments.transition',
];

/**
 * The player entered or left a segment.
 *
 * A signal to re-read, not a source of truth. Membership itself is served by the membership
 * endpoints, and a game must be able to work from those alone: delivery is best-effort, is filtered
 * server-side to players who appear connected, and is not stored for a player who is offline when
 * it is sent. The right reaction is to re-read the player's segments.
 *
 * @remarks
 * Hand-written rather than generated, because `src/__generated__` is produced from the gateway's
 * OpenAPI document and this payload never appears on a REST route — it exists only on the socket.
 * The server-side shape is `SegmentMembershipChanged` in `BeamableAPI`
 * (`BeamableShared/Services/Segmentation/Models/SegmentMembershipChanged.cs`); field casing is
 * camelCase and the two enums serialize as strings because the gateway serializes with
 * `JsonSerializerDefaults.Web` plus `JsonStringEnumConverter`. Keep this in step with that record.
 *
 * @example
 * ```ts
 * beam.on('segments.transition', async (change) => {
 *   console.log(`${change.kind} ${change.segmentId} (${change.cause})`);
 *   // The notification is the trigger; the endpoint is the truth.
 *   const { body } = await realmsGetPlayersSegments(beam.requester, pid, playerId);
 * });
 * ```
 */
export interface SegmentMembershipChanged {
  /** The segment whose membership changed. */
  segmentId: string;
  /** Whether the player entered or left. */
  kind: 'Enter' | 'Exit';
  /**
   * Which input decided it — a rule re-evaluation, an operator editing the include/exclude lists,
   * or the segment itself being disabled or archived (`StateChange`, which exits every member).
   */
  cause: 'Rule' | 'IncludeList' | 'ExcludeList' | 'StateChange';
  /** The segment definition version the change was decided against. */
  ruleVersion: number;
  /** When the change happened, as an ISO-8601 UTC timestamp. */
  timestamp: string;
}
