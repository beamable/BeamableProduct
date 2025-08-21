import { ContentTypeMap } from '@/contents/types/ContentTypeMap';
import { ContentBase } from '@/contents/types/ContentBase';

/**
 * A helper type that removes the last segment of a dot-delimited string.
 * "items.shield.wooden.rare" -> "items.shield.wooden"
 * "items.shield" -> "items"
 * "items" -> ContentBase (as there's no '.' to split on)
 */
type ExtractPrefixes<T extends string> =
  T extends `${infer Prefix}.${infer Rest}`
    ? Rest extends `${string}.${string}`
      ? `${Prefix}.${ExtractPrefixes<Rest>}`
      : Prefix
    : ContentBase;

/**
 * Derives the most specific TypeScript type for a given content ID.
 *
 * This utility searches for a type by first attempting an exact match on the ID.
 * If no direct match is found, it falls back to checking the parent ID
 * (the string with its last segment removed). If no match is found, it defaults
 * to the base `ContentBase` type.
 *
 * @param Id The dot-delimited content identifier.
 *
 * @example
 * ```ts
 * // Assuming ContentTypeMap contains:
 * // "items": ItemContent
 * // "items.shield": ShieldContent
 * // "items.shield.wooden": WoodenShieldContent
 *
 * type T1 = ContentTypeFromId<"items.shield.wooden.rare">; // -> WoodenShieldContent
 * type T2 = ContentTypeFromId<"items.shield.metal">; // -> ShieldContent
 * type T3 = ContentTypeFromId<"items.weapon">; // -> ItemContent
 * type T4 = ContentTypeFromId<"unknown.type">; // -> ContentBase
 * ```
 */
export type ContentTypeFromId<Id extends string> =
  Id extends keyof ContentTypeMap
    ? ContentTypeMap[Id]
    : ExtractPrefixes<Id> extends infer Prefixes
      ? Prefixes extends keyof ContentTypeMap
        ? ContentTypeMap[Prefixes]
        : ContentBase
      : ContentBase;
