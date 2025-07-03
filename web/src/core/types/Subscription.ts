export type Subscription = {
  handler: Function;
  listener: (e: MessageEvent) => void;
  abortController: AbortController;
};
