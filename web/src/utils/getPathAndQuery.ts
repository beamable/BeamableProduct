export function getPathAndQuery(url: string) {
  try {
    const u = new URL(url);
    return u.pathname + u.search;
  } catch {
    // If URL is not valid, return as is. It might be a relative path.
    return url;
  }
}
