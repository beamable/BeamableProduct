export function isBrowserEnv(): boolean {
  return (
    typeof globalThis.window !== 'undefined' &&
    typeof globalThis.window.document !== 'undefined'
  );
}
