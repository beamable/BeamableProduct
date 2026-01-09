export class BeamJsonUtils {
  // Regex to match ISO date strings (e.g., "2025-06-01T14:23:30.123Z")
  private static readonly ISO_DATE_REGEX =
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/;

  // Regex to match a purely numeric string (e.g., "1234567890123")
  private static readonly NUMBER_STRING_REGEX = /^\d+$/;

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
   * Reviver function for JSON.parse that:
   *  - parses ISO date strings into Date
   *  - converts long numeric strings into BigInt
   *
   * Usage:
   *   `JSON.parse(jsonString, BeamJsonUtils.reviver)`
   */
  static reviver(_key: string, value: any): any {
    if (typeof value === 'string') {
      if (BeamJsonUtils.ISO_DATE_REGEX.test(value)) return new Date(value);

      if (BeamJsonUtils.NUMBER_STRING_REGEX.test(value) && value.length > 10)
        return BigInt(value);
    }

    if (typeof value === 'number') {
      const valueAsString = value.toString();
      if (
        BeamJsonUtils.NUMBER_STRING_REGEX.test(valueAsString) &&
        valueAsString.length > 10
      )
        return BigInt(value.toString());
    }

    return value;
  }
}
