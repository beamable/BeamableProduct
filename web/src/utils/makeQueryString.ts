export function makeQueryString(queries: Record<string, unknown>) {
  const entries = Object.entries(queries);
  if (entries.length == 0) return '';

  // An array is sent as one `key=value` pair per element (`states=a&states=b`), not as a comma-joined
  // `states=a%2Cb`: the gateway's `[FromQuery] T[]` binds repeated keys and does not split on commas.
  const encodedQueries = entries
    .filter(([, value]) => value !== undefined)
    .flatMap(([key, value]) =>
      (Array.isArray(value) ? value : [value])
        .filter((element) => element !== undefined)
        .map((element) => `${key}=${encodeURIComponent(String(element))}`),
    );

  if (encodedQueries.length === 0) return '';

  return '?'.concat(encodedQueries.join('&'));
}
