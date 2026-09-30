import type { NotificationEventMap } from './NotificationEventMap';
import type { RefreshableServiceMap } from './RefreshableServiceMap';
import type { Subscription } from './Subscription';
import { ServerEventType } from '@/core/types/ServerEventType';

/** Every context `beam.on` accepts: a refreshable service, or a pass-through notification. */
export type BeamClientContext =
  | keyof RefreshableServiceMap
  | keyof NotificationEventMap;

/**
 * The data a handler for `K` receives — the re-fetched service data for a refreshable context, the
 * notification payload itself for a notification context.
 */
export type BeamClientContextData<K extends BeamClientContext> =
  K extends keyof RefreshableServiceMap
    ? RefreshableServiceMap[K]['data']
    : K extends keyof NotificationEventMap
      ? NotificationEventMap[K]
      : never;

/** `ClientSubscriptionMap` maps a client context to an array of active subscriptions for it. */
export type ClientSubscriptionMap = Record<BeamClientContext, Subscription[]>;

/** `ServerSubscriptionMap` maps a server event type to an array of active subscriptions for that event. */
export type ServerSubscriptionMap = Record<ServerEventType, Subscription[]>;
