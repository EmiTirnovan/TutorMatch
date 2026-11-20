// Parts of a Romanian address that OpenStreetMap never stores (apartment, floor, staircase, intercom, room).
// The value must hold a digit (C309, 18A), be a single letter, or be "parter"/"mansarda" so street names such as
// "Strada Scolii" or "Sala Palatului" are not mistaken for "sc." / "sala".
const UNIT_PATTERN =
  /\b(?:ap(?:artament)?|apt|et(?:aj)?|sc(?:ara)?|interfon|cam(?:era)?|sala)(?:\.\s*|\s+)(?:[a-z]?\d+[a-z]?\b|[a-z]\b|parter\b|mansarda\b)/giu;
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
