/**
 * Recursively freezes an object (or array) and all of its nested properties.
 * Returns the same object, now immutable.
 */
export function deepFreeze<T>(obj: T): T {
  // Only freeze objects, skip null
  if (obj === null || typeof obj !== 'object') {
    return obj;
  }

  // Retrieve property names and symbols
  const propNames: (string | symbol)[] = [
    ...Object.getOwnPropertyNames(obj),
    ...Object.getOwnPropertySymbols(obj),
  ];

  // Freeze nested props first
  for (const name of propNames) {
    const value = Reflect.get(obj, name);
    if (value && typeof value === 'object' && !Object.isFrozen(value)) {
      deepFreeze(value);
    }
  }

  // Finally freeze self (shallow)
  return Object.freeze(obj);
}
