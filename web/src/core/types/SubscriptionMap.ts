import { ContextMap } from './ContextMap';
import { Subscription } from './Subscription';

export type SubscriptionMap = Record<keyof ContextMap, Subscription[]>;
