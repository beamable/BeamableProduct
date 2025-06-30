type ValueType = string | bigint;

export function endpointEncoder(value: ValueType): string {
  return encodeURIComponent(value.toString());
}
