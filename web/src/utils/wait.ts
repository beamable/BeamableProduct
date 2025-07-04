/** Pauses execution for a given number of milliseconds. */
export function wait(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise<void>((resolve, reject) => {
    const id = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort);
      resolve();
    }, ms);

    const onAbort = () => {
      clearTimeout(id);
      reject(new Error('Delay aborted'));
    };

    if (signal) signal.addEventListener('abort', onAbort, { once: true });
  });
}
