import type { RefreshableServiceMap } from './RefreshableServiceMap';
import type { Subscription } from './Subscription';

/** `SubscriptionMap` maps a refreshable service context to an array of active subscriptions for that context. */
export type SubscriptionMap = Record<
  keyof RefreshableServiceMap,
  Subscription[]
>;
