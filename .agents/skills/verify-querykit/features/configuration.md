# Configuration

A developer passes a `QueryKitConfiguration` to change how QueryKit reads the input. The configuration can rename properties, block properties, add derived properties and custom operations, replace the operator symbols, allow unknown properties, limit the property depth, and select `ToUpper()` for case-insensitive operators.

## Sub-features

- `config-query-name` makes an alias for a property with `HasQueryName` (`name` for `Title`, `chef` for `Author.Name`).
- `config-prevent-filter` blocks a filter on a property with `PreventFilter()`.
- `config-prevent-sort` blocks a sort on a property with `PreventSort()`.
- `config-derived` adds a computed property with `DerivedProperty`.
- `config-custom-operation` adds a named operation with `CustomOperation`.
- `config-custom-operators` replaces the operator symbols (`eq`, `gt`, `and`, `or`, appendix `i`).
- `config-allow-unknown` ignores unknown properties with `AllowUnknownProperties = true`.
- `config-max-depth` limits the depth of dotted paths with `MaxPropertyDepth`.
- `config-upper` uses `ToUpper()` for case-insensitive operators with `CaseInsensitiveComparison = CaseInsensitiveMode.Upper`.
- `config-parameterized` sends filter values as SQL parameters with `ParameterizeFilterValues = true`.
- `config-remove-ignored` drops an ignored clause instead of replacing it with `True == True`, with `IgnoredClauseBehavior = IgnoredClauseBehavior.Remove`.
- `config-small-limits` lowers the parse limits with `MaxInputLength` and `MaxNestingDepth`.
- `config-nesting-depth` counts each parenthesized group against `MaxNestingDepth`. A `(` or `)` inside a quoted value does not change the count.

## How to get to it (user POV)

- Create `new QueryKitConfiguration(settings => { ... })` and pass it to `ApplyQueryKitFilter`, `ApplyQueryKitSort`, or `QueryKitData.Configuration`.
- The driver builds each configuration from a named preset. Run `qk configs` to list the presets. The code is in `harness/Driver/Configs.cs`.

## Driving it with qk

Preconditions:

- A run is up and `qk doctor` prints only `ok` lines.
- The seed data matches `features/README.md`.
- `qk configs` lists `aliases`, `loose-names`, `derived`, `custom-operation`, `word-operators`, `hidden-price`, `allow-unknown`, `max-depth-0`, `upper`, `parameterized`, `remove-ignored`, `small-limits`, and `depth-10`.

- **Query names.** Run `qk run configuration-query-name --config aliases --filter 'chef == "Julia Child" && name _= "S"'`. Both targets give `["Salt Bread"]`.
- **Query names that are not identifiers.** Run `qk run configuration-loose-query-names --config loose-names --filter 'recipe-title == "Pancakes" || _stars > 4 || chef name == "Gordon Ramsay"'`. Both targets give `["Pancakes", "Beef Stew"]`.
- **Prevent filter.** Run `qk run configuration-prevent-filter --config aliases --filter 'Rating > 1'`. Exit `0`. Both targets give all four recipes. The `expression` is `x => (True == True)`.
- **Prevent sort.** Run `qk run configuration-prevent-sort --config aliases --sort 'Price'`. Exit `0`. Both targets give the seed order `["Pancakes", "Beef Stew", "Salt Bread", "Plain Water"]`, not the price order.
- **Derived properties.** Run `qk run configuration-derived --config derived --filter 'headline @=* "julia" && top_rated == true'`. Both targets give `Pancakes` and `Salt Bread`. The `sql` contains `|| ' by ' ||`.
- **Custom operation.** Run `qk run configuration-custom-operation --config custom-operation --filter 'total_stock_above > 10'`. Both targets give `["Beef Stew"]`. Its ingredient stock totals 52.
- **Custom operators.** Run `qk run configuration-custom-operators --config word-operators --filter 'Title eqi "pancakes" or Rating gt 3'`. Both targets give `Pancakes` and `Salt Bread`.
- **Allow unknown.** Run `qk run configuration-allow-unknown --config allow-unknown --filter 'Nope == 1 && Rating > 3'`. Exit `0`. Both targets give `Pancakes` and `Salt Bread`.
- **Max depth.** Run `qk run configuration-max-depth --config max-depth-0 --filter 'Author.Name == "Julia Child"'`. Exit `2`. Both targets have `error.type` `QueryKit.Exceptions.QueryKitPropertyDepthExceededException`.
- **Upper mode.** Run `qk run configuration-upper --config upper --filter 'Title @=* "bread"'`. Both targets give `["Salt Bread"]`. The `sql` contains `upper(r."Title")`.
- **Parameterized values.** Run `qk run configuration-parameterized --config parameterized --filter 'Title == "Pancakes"'`. Both targets give `["Pancakes"]`. The `sql` contains a `@` parameter instead of the literal `'Pancakes'`.
- **Remove ignored clauses.** Run `qk run configuration-remove-ignored --config remove-ignored --filter 'Rating > 1 && Nope == 1'`. Exit `0`. Both targets give all four recipes. The `expression` has no `True == True`.
- **Small parse limits.** Run `qk run configuration-small-limits --config small-limits --filter '((((Title == "Pancakes"))))'`. Exit `2`. Both targets have `error.type` `QueryKit.Exceptions.QueryKitNestingDepthExceededException`.
- **Quoted parentheses and the nesting depth.** Run `qk run configuration-nesting-depth --config depth-10 --filter 'Title == "))))))))))))))))))))" || ((((((((((((((((((((Title == "Pancakes"))))))))))))))))))))'`. Exit `2`. Both targets have `error.type` `QueryKit.Exceptions.QueryKitNestingDepthExceededException` with the message `The filter has a nesting depth of 11, which exceeds the maximum allowed depth of 10.`

## Gotchas

- `PreventFilter()` and `PreventSort()` do not throw. The blocked condition becomes `True == True`, and the blocked sort is ignored. Assert the rows, not an error.
- The Postgres `sql` for `config-prevent-sort` has no `"Price"` key. It has only the `ORDER BY r."Id", a."Id"` that EF Core adds.
- Custom operators need spaces around them. `Title eqi"pancakes"` does not parse.
- An alias replaces the property name only in QueryKit input. `Title` still works next to the `name` alias.
- To prove a configuration that no preset covers, add a preset to `harness/Driver/Configs.cs`, run `qk rebuild`, and add it to this file.
