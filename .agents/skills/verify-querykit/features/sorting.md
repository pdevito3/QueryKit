# Sorting

A developer passes a comma-separated sort string to `ApplyQueryKitSort`, or passes a filter and a sort together in `QueryKitData` to `ApplyQueryKit`. The result order is the same on an in-memory list and in Postgres.

## Sub-features

- `sort-direction` sorts with `asc` (the default) or `desc`.
- `sort-sieve-prefix` sorts descending with a `-` prefix (`-Rating`).
- `sort-multi` sorts on more than one key, including a nested property (`Author.Name`).
- `sort-aggregate` applies a filter, a sort, and a configuration in one `ApplyQueryKit(QueryKitData)` call.

## How to get to it (user POV)

- Call `queryable.ApplyQueryKitSort(sort, config?)` on an `IQueryable<T>`, or `enumerable.ApplyQueryKitSort(sort, config?)` on an `IEnumerable<T>`.
- Call `source.ApplyQueryKit(new QueryKitData { Filters, SortOrder, Configuration })`.

## Driving it with qk

Preconditions:

- A run is up and `qk doctor` prints only `ok` lines.
- The seed data matches `features/README.md`.

- **Descending.** Run `qk run sorting-desc --sort 'Rating desc'`. Both targets give `["Pancakes", "Salt Bread", "Beef Stew", "Plain Water"]`. The `sql` contains `ORDER BY r."Rating" DESC, r."Id"`.
- **Default ascending.** Run `qk run sorting-asc --sort 'Price'`. Both targets give `["Plain Water", "Salt Bread", "Pancakes", "Beef Stew"]`.
- **Nested key and Sieve prefix.** Run `qk run sorting-multi --sort 'Author.Name, -Rating'`. Both targets give `["Plain Water", "Beef Stew", "Pancakes", "Salt Bread"]`.
- **Filter then sort.** Run `qk run sorting-filter-then-sort --filter 'Author.Name == "Julia Child"' --sort 'Title desc'`. Both targets give `["Salt Bread", "Pancakes"]`.
- **Aggregate with a configuration.** Run `qk run sorting-aggregate --aggregate --config aliases --filter 'chef == "Julia Child"' --sort 'name desc'`. The `input.api` is `ApplyQueryKit(QueryKitData)`. Both targets give `["Salt Bread", "Pancakes"]`.

## Gotchas

- EF Core adds `r."Id"` and `a."Id"` after the QueryKit sort keys, because the driver uses split-query mode. Only the keys before them come from QueryKit.
- A sort on a property with `PreventSort()` is ignored without an error. The rows come back in source order (see `configuration.md`).
- An unknown sort property throws `SortParsingException` (see `error-handling.md`).
