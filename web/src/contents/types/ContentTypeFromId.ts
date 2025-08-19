import { ContentTypeMap } from '@/contents/types/ContentTypeMap';
import { ContentBase } from '@/contents/types/ContentBase';

/**
 * @internal
 * Recursively removes the last segment of a dot-separated string. e.g., 'a.b.c' -> 'a.b'
 */
export type RemoveLastSegment<S extends string> =
  S extends `${infer Head}.${infer Tail}` // Split the string at the first dot into a Head and Tail
    ? Tail extends `${string}.${string}` // Check if the Tail ('b.c') also contains a dot
      ? `${Head}.${RemoveLastSegment<Tail>}` // If yes, keep the Head and recurse on the Tail
      : Head // If no, return the Head as it's the second-to-last segment
    : S; // If the string has no dots, return it as-is

/**
 * Maps a content ID string to its most specific TypeScript type.
 * It searches for a match by progressively removing the last segment of the ID.
 */
export type ContentTypeFromId<Id extends string> =
  Id extends keyof ContentTypeMap // Check if the full ID is an exact key in the type map
    ? ContentTypeMap[Id] // Then return the mapped type
    : Id extends `${string}.${string}` // If not, check if the ID has a dot
      ? ContentTypeFromId<RemoveLastSegment<Id>> // If yes, recurse with the last segment stripped off
      : ContentBase; // If there are no more dots or no matches were found, fall back to ContentBase
