// Parts of an address that OpenStreetMap never stores (apartment, floor, staircase, intercom, room),
// in Romanian (with or without diacritics) and English.
// The value must hold a digit (C309, 18A), be a single letter, or be a named floor so street names such as
// "Strada Scolii" or "Sala Palatului" are not mistaken for "sc." / "sala".
// JS \b only knows ASCII letters, so boundaries are Unicode lookarounds (otherwise "mansardă" would not end a word).
const START = String.raw`(?<![\p{L}\p{N}])`;
const END = String.raw`(?![\p{L}\p{N}])`;
const UNIT_WORDS = String.raw`ap|apt|apartament(?:ul)?|apartment|et|etaj(?:ul)?|sc|scar[aă]|interfon|cam|camer[aă]|sal[aă]|flat|floor|fl|room|rm|unit|suite`;
const UNIT_VALUES = String.raw`[a-z]?\d+[a-z]?|[a-z]|parter|mansard[aă]|ground`;
const UNIT_PATTERN = new RegExp(
  `${START}(?:(?:${UNIT_WORDS})(?:\\.\\s*|\\s+)(?:${UNIT_VALUES})|\\d+(?:st|nd|rd|th)\\s+floor|ground\\s+floor)${END}`,
  'giu'
);
const BLOCK_PATTERN = /\bbl(?:oc)?(?:\.\s*|\s+)[a-z0-9-]+/giu;
const NUMBER_PATTERN = /\b(?:nr\.?\s*)?\d{1,4}[a-z]?\b/giu;

export interface SplitAddress {
  /** What can be geocoded: street, number, block, city. */
  query: string;
  /** Unit details removed from the query, comma separated ('' when none). */
  details: string;
}

export function splitAddress(text: string): SplitAddress {
  const details = text.match(UNIT_PATTERN) ?? [];
  return {
    query: tidy(text.replace(UNIT_PATTERN, '')),
    details: details.map((d) => d.trim()).join(', '),
  };
}

/**
 * Queries to try in order, most precise first: as typed, then without the block, then the street alone.
 * `streetOnly` marks the last one, where the pin lands on the street rather than the building.
 */
export function fallbackQueries(query: string): { query: string; streetOnly: boolean }[] {
  const withoutBlock = tidy(query.replace(BLOCK_PATTERN, ''));
  const street = tidy(withoutBlock.replace(NUMBER_PATTERN, ''));
  const queries = [
    { query, streetOnly: false },
    { query: withoutBlock, streetOnly: false },
    { query: street, streetOnly: true },
  ];
  return queries.filter(
    (q, i) => q.query.length >= 3 && queries.findIndex((o) => o.query === q.query) === i,
  );
}

/** Drops the empty comma slots left behind after removing parts. */
function tidy(text: string): string {
  return text
    .split(',')
    .map((part) => part.replace(/\s+/g, ' ').trim())
    .filter(Boolean)
    .join(', ');
}
