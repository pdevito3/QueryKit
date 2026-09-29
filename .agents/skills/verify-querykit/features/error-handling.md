# Error handling

A developer catches `QueryKitException` to return a `400` for bad input. QueryKit throws a specific subtype for each kind of bad filter or sort input. The exception occurs when the query is built, before it reaches the database.

## Sub-features

- `error-parsing` throws `ParsingException` for bad syntax: an unknown operator, a missing quote, a missing parenthesis, or a missing value.
- `error-unknown-filter-property` throws `UnknownFilterPropertyException` for an unknown filter property.
- `error-sort` throws `SortParsingException` for an unknown sort property.
- `error-depth` throws `QueryKitPropertyDepthExceededException` when a path is deeper than `MaxPropertyDepth` (see `configuration.md`).

## How to get to it (user POV)

- Wrap `ApplyQueryKitFilter`, `ApplyQueryKitSort`, or `ApplyQueryKit` in `try` and `catch (QueryKitException)`.

## Driving it with qk

Preconditions:

- A run is up and `qk doctor` prints only `ok` lines.

- **Bad operator.** Run `qk run error-handling-bad-operator --filter 'Title === "x"'`. Exit `2`. Both targets have `error.type` `QueryKit.Exceptions.ParsingException` and `isQueryKitException` `true`.
- **Missing quote.** Run `qk run error-handling-missing-quote --filter 'Title == "unterminated'`. Exit `2`. Both targets have `ParsingException`. The message contains `expected "`.
- **Missing parenthesis.** Run `qk run error-handling-missing-paren --filter '(Rating > 1'`. Exit `2`. Both targets have `ParsingException`. The message contains `expected )`.
- **Unknown filter property.** Run `qk run error-handling-unknown-property --filter 'Nope == 1'`. Exit `2`. Both targets have `QueryKit.Exceptions.UnknownFilterPropertyException` with the message `The filter property 'Nope' was not recognized.`
- **Unknown sort property.** Run `qk run error-handling-unknown-sort --sort 'Nope desc'`. Exit `2`. Both targets have `QueryKit.Exceptions.SortParsingException` with the message `Parsing failed during sorting. 'Nope' was not recognized.`

## Gotchas

- Some bad input escapes the `QueryKitException` hierarchy. `Rating > "abc"` throws `System.FormatException`, and `Title @= null` throws `System.ArgumentNullException` on the memory target. The driver exits `1` for these. A consumer that catches only `QueryKitException` returns a `500` for them.
- An unquoted string value such as `Title == salt` is not an error. It is a literal (see `filtering.md`).
- On the Postgres target, the `sql` field is absent when QueryKit throws. This is correct, because the query was never built.
