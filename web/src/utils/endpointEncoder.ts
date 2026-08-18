type ValueType = string | number | bigint;

export function endpointEncoder(value: ValueType): string {
  return encodeURIComponent(value.toString());
}
