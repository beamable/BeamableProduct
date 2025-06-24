export type PromiseWithResolversPolyfill<T = void> = {
  promise: Promise<T>;
  resolve: (value: T | PromiseLike<T>) => void;
  reject: (reason?: unknown) => void;
};

/**
 * Creates a promise whose resolve/reject functions can be called later.
 * @template T - The type of the value that the promise will resolve with.
 * @returns {PromiseWithResolversPolyfill<T>} An object containing the promise and its resolve/reject functions.
 */
export function promiseWithResolvers<
  T = void,
>(): PromiseWithResolversPolyfill<T> {
  let resolve!: (value: T | PromiseLike<T>) => void;
  let reject!: (reason?: unknown) => void;

  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });

  return { promise, resolve, reject };
}
