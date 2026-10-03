# QueryKit verification map

This directory is the maintained source for verifying the user-facing behavior of QueryKit. Read this index before you drive the library. Then use the matching feature file as the recipe.

## Baseline preconditions

- Start a run with `eval "$(.agents/skills/verify-querykit/qk up)"` from the repo root.
- Run `.agents/skills/verify-querykit/qk doctor` and make sure that every line starts with `ok`.
- Never drive a run that this verification did not start.

In the commands below, `qk` means `.agents/skills/verify-querykit/qk`.

## Seed data

The driver model is `Recipe`. Every recipe has one `Author` and zero or more `Ingredients`. The seed data is fixed in `harness/Driver/Model.cs`.

| Title | Author.Name | Rating | Price | IsVegetarian | Visibility | CreatedAt (UTC) | DateOfOrigin | Sku | Serving | ServeTime | Directions | Tags | Ingredients (Name:Stock) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Pancakes | Julia Child | 5 | 4.50 | true | Public (1) | 2024-01-15 08:00 | 1900-01-01 | 001 | Warm, with syrup | 08:30:00.5 | Whisk and fry | breakfast, sweet | flour:10, egg:0 |
| Beef Stew | Gordon Ramsay | 3 | 12.00 | false | Public (1) | 2024-03-01 18:30 | null | 002 | Hot | 18:00:00 | Simmer for hours | dinner | beef:2, salt:50 |
| Salt Bread | Julia Child | 4 | 3.25 | true | Private (2) | 2024-06-10 12:00 | 1950-05-20 | 003 | Sliced | 12:15:30.25 | Knead and bake | bread, Winner | salt:5, flour:3 |
| Plain Water | Anonymous | 1 | 0.00 | true | Private (2) | 2023-12-31 23:59 | null | 004 | null | null | null | (none) | (none) |

`CreatedAt` is a `timestamp with time zone` column. `LocalCreatedAt` holds the same wall-clock time as `CreatedAt` in a `timestamp without time zone` column.

## Driving conventions

- Start every recipe from the baseline. The driver only reads data, so no recipe changes the seed rows.
- Use `--target both` (the default) for every proof. Both targets must give the same `titles`.
- Both targets return rows in seed order when no sort applies. The memory target keeps list order. On Postgres, EF Core adds `r."Id"` as the last `ORDER BY` key, and the seed ids follow the table order.
- Put filter and sort strings in single quotes in the shell. Keep the text inside the quotes exact.
- Use a `qk run` label that starts with the feature file name, for example `filtering-in-operator`.
- Read `titles` for the result. Read `expression` and `sql` to see how QueryKit translated the input.
- After a change to `QueryKit/`, run `qk rebuild` and then `qk doctor` before the next drive.

## Proof and skip reporting

- A proof is the set of `.cmd`, `.json`, and `.stderr` files for each label in `qk evidence`.
- A filter proof shows the expected `titles` on both targets and a matching `WHERE` clause in `sql`.
- A sort proof shows the full order of `titles` on both targets.
- An error proof shows `error.type`, `isQueryKitException`, and the exit code.
- Record the git revision from the last line of `qk doctor` with the proof.
- If a sub-feature cannot be driven, report the command that you tried and the missing prerequisite.
- Do not report a sub-feature as verified through a different sub-feature.

## Feature entry contract

Each feature file starts with an H1 title and one paragraph about the user-visible behavior. Then it has exactly four H2 sections in this order:

1. `Sub-features` lists short IDs, with one line for each behavior.
2. `How to get to it (user POV)` lists each API entry point that a developer uses.
3. `Driving it with qk` starts with `Preconditions:`. Then labeled bullets give the user action, the exact command, and the observable result.
4. `Gotchas` lists traps that can waste or invalidate a verification run.

Keep implementation details out of the map. Name only API entry points, filter syntax, commands, and observable proof.

## Features

- [Filtering](./filtering.md) covers comparison operators, logical operators, parentheses, case-insensitive operators, the in operator, typed values, property grouping, property-to-property comparisons, and arithmetic.
- [Sorting](./sorting.md) covers ascending and descending sorts, the Sieve `-` prefix, nested sort keys, and `ApplyQueryKit` with `QueryKitData`.
- [Collections and nested objects](./collections-and-nested.md) covers navigation properties, `Any` and `All` over child collections, count operators, and primitive list operators.
- [Configuration](./configuration.md) covers query names, prevent filter and prevent sort, derived properties, custom operations, custom operators, unknown properties, max depth, and the case-insensitive mode.
- [Error handling](./error-handling.md) covers the exception types that a consumer catches to return a `400`.

## Not mapped yet

These README features are not in the map. The harness cannot drive them without changes:

- SoundEx (`~~`, `!~`) needs a `SOUNDEX` database function and `DbContextType` in the configuration.
- `HasConversion` support needs a value-object property with an EF Core conversion in the model.
- Filtering projections and raw SQL projections need a projection query in the driver.
