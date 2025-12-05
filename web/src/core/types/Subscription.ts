/** Represents a single subscription to a real-time event. */
export type Subscription = {
  handler: Function;
  listener: (e: MessageEvent) => void;
  abortController?: AbortController;
};
