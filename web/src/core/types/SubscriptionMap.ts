import { RefreshableServiceMap } from './RefreshableServiceMap';
import { Subscription } from './Subscription';

export type SubscriptionMap = Record<
  keyof RefreshableServiceMap,
  Subscription[]
>;
