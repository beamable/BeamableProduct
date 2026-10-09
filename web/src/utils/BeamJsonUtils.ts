export class BeamJsonUtils {
  // Regex to match ISO date strings (e.g., "2025-06-01T14:23:30.123Z")
  private static readonly ISO_DATE_REGEX =
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/;

  // Regex to match a purely numeric string, optionally negative (e.g., "1234567890123", "-1234567890123")
  private static readonly NUMBER_STRING_REGEX = /^-?\d+$/;

  // Integers with more digits than this may lose precision as JavaScript Numbers.
  private static readonly MAX_SAFE_DIGITS = 10;

  /**
   * Replacer function for JSON.stringify that:
   *  - serializes BigInt as string
   *  - serializes Date as ISO string
   *
   * Usage:
   *   `JSON.stringify(obj, BeamJsonUtils.replacer)`
   */
  static replacer(_key: string, value: any): any {
    if (typeof value === 'bigint') return value.toString();
    if (value instanceof Date) return value.toISOString();
    return value;
  }

  /**
   * Pre-processes raw JSON text by quoting large integers (>10 digits)
   * so they are not silently rounded by JSON.parse.
   *
   * Scans the text rather than matching a pattern, so digits inside a string
   * value are never touched — including a string that itself holds JSON
   * (e.g. `"payload":"{\"id\":70820408384930816}"`), where quoting would
   * insert unescaped quotes and break the whole document.
   */
  static quoteLargeInts(text: string): string {
    let out = '';
    let start = 0;
    let inString = false;
    for (let i = 0; i < text.length; i++) {
      const c = text[i];
      if (inString) {
        if (c === '\\') i++;
        else if (c === '"') inString = false;
        continue;
      }
      if (c === '"') {
        inString = true;
        continue;
      }
      if (c !== '-' && (c < '0' || c > '9')) continue;

      let end = i + 1;
      while (end < text.length && /[0-9.eE+-]/.test(text[end])) end++;
      const token = text.slice(i, end);
      const digits = token.startsWith('-') ? token.length - 1 : token.length;
      if (/^-?\d+$/.test(token) && digits > BeamJsonUtils.MAX_SAFE_DIGITS) {
        out += text.slice(start, i) + '"' + token + '"';
        start = end;
      }
      i = end - 1;
    }
    return out + text.slice(start);
  }

  /**
   * Parses a JSON string with safe handling for large integers and dates.
   * Large integers are quoted before parsing to prevent precision loss,
   * then converted to BigInt by the reviver.
   */
  static parse(text: string): any {
    return JSON.parse(BeamJsonUtils.quoteLargeInts(text), BeamJsonUtils.reviver);
  }

  /**
   * Reviver function for JSON.parse that:
   *  - parses ISO date strings into Date
   *  - converts long numeric strings into BigInt
   *
   * Usage:
   *   `BeamJsonUtils.parse(jsonString)` (preferred — handles precision safely)
   *   `JSON.parse(jsonString, BeamJsonUtils.reviver)` (only if text is pre-processed)
   */
  static reviver(_key: string, value: any): any {
    if (typeof value === 'string') {
      if (BeamJsonUtils.ISO_DATE_REGEX.test(value)) return new Date(value);

      if (BeamJsonUtils.NUMBER_STRING_REGEX.test(value) && value.length > 10)
        return BigInt(value);
    }

    return value;
  }
}
