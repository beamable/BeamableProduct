export function makeQueryString(queries: Record<string, unknown>) {
  const entries = Object.entries(queries);
  if (entries.length == 0) return '';

  return '?'.concat(
    Object.entries(queries)
      .filter(([, value]) => value !== undefined)
      .map(([key, value]) => `${key}=${encodeURIComponent(String(value))}`)
      .join('&'),
  );
}
