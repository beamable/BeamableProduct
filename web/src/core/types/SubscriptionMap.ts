import type { RefreshableServiceMap } from './RefreshableServiceMap';
import type { Subscription } from './Subscription';
import { ServerEventType } from '@/core/types/ServerEventType';

/** `ClientSubscriptionMap` maps a refreshable service context to an array of active subscriptions for that context. */
export type ClientSubscriptionMap = Record<
  keyof RefreshableServiceMap,
  Subscription[]
>;

/** `ServerSubscriptionMap` maps a server event type to an array of active subscriptions for that event. */
export type ServerSubscriptionMap = Record<ServerEventType, Subscription[]>;
