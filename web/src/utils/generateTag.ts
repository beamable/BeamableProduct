/**
 * Generates a random alphanumeric beam tag used for namespacing token storage keys and BroadcastChannel name.
 * Defaults to 8 characters.
 */
export function generateTag(length = 8): string {
  const chars =
    'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  const max = chars.length;
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * max));
  }
  return result;
}
