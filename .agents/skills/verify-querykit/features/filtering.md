# Filtering

A developer passes a filter string to `ApplyQueryKitFilter`. QueryKit parses the string into a LINQ predicate. The same string gives the same rows from an in-memory list and from a Postgres table through EF Core.

## Sub-features

- `filter-compare` compares a property with a literal (`==`, `!=`, `>`, `<`, `>=`, `<=`).
- `filter-string` matches text (`@=`, `_=`, `_-=`, and the negated forms).
- `filter-case-insensitive` adds `*` to a string operator (`@=*`, `==*`).
- `filter-logic` combines conditions with `&&`, `||`, and parentheses.
- `filter-in` matches a list of values (`^^`, `!^^`).
- `filter-typed` filters `bool`, enum (by integer value), `DateTime`, and nullable values.
- `filter-group` applies one comparison to a list of properties (`(A, B) @=* "x"`).
- `filter-property-compare` compares two properties (`Rating > Price`).
- `filter-arithmetic` compares an arithmetic expression (`(Price * 2) > 10`).

## How to get to it (user POV)

- Call `queryable.ApplyQueryKitFilter(filter, config?)` on an EF Core `IQueryable<T>`.
- Call `enumerable.ApplyQueryKitFilter(filter, config?)` on an `IEnumerable<T>`.
- Call `FilterParser.ParseFilter<T>(filter, config?)` to get the `Expression<Func<T, bool>>`. The driver prints this expression as `expression`.

## Driving it with qk

Preconditions:

- A run is up and `qk doctor` prints only `ok` lines.
- The seed data matches `features/README.md`.

- **Compare and case-insensitive contains.** Filter on two conditions. Run `qk run filtering-contains --filter 'Title @=* "salt" && Rating > 3'`. Exit `0`. Both targets give `["Salt Bread"]`. The `sql` contains `WHERE lower(r."Title") LIKE '%salt%' AND r."Rating" > 3`.
- **Starts with.** Run `qk run filtering-starts-with --filter 'Title _= "P"'`. Both targets give `["Pancakes", "Plain Water"]`.
- **Logic and parentheses.** Run `qk run filtering-logic --filter '(IsVegetarian == true && Rating < 2) || Title == "Beef Stew"'`. Both targets give `["Beef Stew", "Plain Water"]`.
- **In operator.** Run `qk run filtering-in --filter 'Rating ^^ [3, 5]'`. Both targets give `["Pancakes", "Beef Stew"]`.
- **Enum by integer.** Run `qk run filtering-enum --filter 'Visibility == 2'`. Both targets give `["Salt Bread", "Plain Water"]`. The `expression` shows `x.Visibility == Private`.
- **Date and null.** Run `qk run filtering-date-null --filter 'CreatedAt >= "2024-03-01T00:00:00Z" && DateOfOrigin != null'`. Both targets give `["Salt Bread"]`.
- **Property grouping.** Run `qk run filtering-group --filter '(Title, Directions) @=* "bake"'`. Both targets give `["Salt Bread"]`.
- **Property to property.** Run `qk run filtering-property-compare --filter 'Rating > Price'`. Both targets give `["Pancakes", "Salt Bread", "Plain Water"]`.
- **Arithmetic.** Run `qk run filtering-arithmetic --filter '(Price * 2) > 10'`. Both targets give `["Beef Stew"]`.

## Gotchas

- Without `--sort`, both targets return rows in seed order. The Postgres order comes from the `ORDER BY r."Id"` that EF Core adds, not from QueryKit.
- An unquoted value such as `Title == salt` does not throw. QueryKit reads it as the literal `"salt"` and returns no rows.
- `Title @= null` throws `System.ArgumentNullException` on the memory target. This is not a `QueryKitException`.
- `Rating > "abc"` throws `System.FormatException`, not a `QueryKitException`. The driver exits `1`.
- The case-insensitive operators use `lower()` in SQL by default. The `upper` preset changes this (see `configuration.md`).
