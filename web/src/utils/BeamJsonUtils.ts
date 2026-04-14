export class BeamJsonUtils {
  // Regex to match ISO date strings (e.g., "2025-06-01T14:23:30.123Z")
  private static readonly ISO_DATE_REGEX =
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/;

  // Regex to match a purely numeric string, optionally negative (e.g., "1234567890123", "-1234567890123")
  private static readonly NUMBER_STRING_REGEX = /^-?\d+$/;

  // Matches JSON integer values longer than 10 digits that would lose precision
  // as JavaScript Numbers. Quotes them so they arrive at the reviver as strings.
  // Uses negative lookbehind/lookahead to avoid matching inside quoted strings.
  private static readonly UNSAFE_INT_REGEX =
    /(?<=[:,\[{]\s*)-?\d{11,}(?=\s*[,\]\}])/g;

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
   */
  static quoteLargeInts(text: string): string {
    return text.replace(BeamJsonUtils.UNSAFE_INT_REGEX, '"$&"');
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
