# Collections and nested objects

A developer filters on a property of a related object with a dotted path, and on child collections. By default a collection filter matches when any child matches. A `%` prefix makes it match only when all children match. A `#` prefix compares the count of the collection.

## Sub-features

- `nested-navigation` filters on a navigation property (`Author.Name`).
- `collection-any` matches when any child matches (`Ingredients.Name == "salt"`).
- `collection-all` matches when all children match (`Ingredients.Stock %>= 1`).
- `collection-count` compares the number of children (`Ingredients #== 0`).
- `collection-primitive-has` matches a value in a primitive list (`Tags ^$`, `Tags ^$*`, `Tags !^$`).

## How to get to it (user POV)

- Call `ApplyQueryKitFilter` with a dotted property path or a collection operator.
- The same syntax works on `IQueryable<T>` and on `IEnumerable<T>`.

## Driving it with qk

Preconditions:

- A run is up and `qk doctor` prints only `ok` lines.
- The seed data matches `features/README.md`.

- **Navigation property.** Run `qk run collections-navigation --filter 'Author.Name == "Julia Child"'`. Both targets give `Pancakes` and `Salt Bread`. The `sql` has `INNER JOIN "Authors"` and a `WHERE` on `a."Name"`.
- **Any child.** Run `qk run collections-any --filter 'Ingredients.Name == "salt"'`. Both targets give `Beef Stew` and `Salt Bread`. The `expression` contains `.Any(`.
- **All children.** Run `qk run collections-all --filter 'Ingredients.Stock %>= 1'`. Both targets give `Beef Stew`, `Salt Bread`, and `Plain Water`. The `expression` contains `.All(`.
- **Count.** Run `qk run collections-count --filter 'Ingredients #== 0'`. Both targets give `["Plain Water"]`.
- **Primitive list, case-insensitive.** Run `qk run collections-has --filter 'Tags ^$* "winner"'`. Both targets give `["Salt Bread"]`. The stored tag is `Winner`.

## Gotchas

- `All` is true for an empty collection. So `Plain Water`, which has no ingredients, matches `Ingredients.Stock %>= 1`.
- `Tags ^$ "winner"` without `*` does not match `Winner`. Use `^$*` for a case-insensitive match.
- The driver adds `Include` for `Author` and `Ingredients` after QueryKit runs. The first statement in `sql` is the filter query. EF Core loads the ingredients in a second statement.
