---
name: verify-querykit
description: Drive the QueryKit library the way a consumer app does and capture proof. The qk harness runs filter and sort strings through the public API (ApplyQueryKitFilter, ApplyQueryKitSort, ApplyQueryKit) against an in-memory IEnumerable and a real Postgres database through EF Core. Use it to show that a QueryKit change or bug fix works end to end, to reproduce a reported filter or sort bug, or to prove a behavior before a release. Unit tests are not a substitute for this proof.
---

# Verify QueryKit

QueryKit is a .NET library. It has no UI and no server. Its user is a developer who passes a filter or sort string to an extension method and runs the query. This skill reproduces that path.

The surface to drive is the public C# API on `IEnumerable<T>` and on an EF Core `IQueryable<T>` with Postgres. Other surfaces in the repo are not the product:

- `QueryKit.WebApiTestProject` has no endpoints. It only holds the `DbContext` for the integration tests.
- `QueryKit.UnitTests` and `QueryKit.IntegrationTests` are test suites. Run them as a separate check. They do not replace a live proof.

## What the harness is

Everything lives in this skill directory:

- `qk` is the harness script. Run it from any directory.
- `harness/Driver/` is a small console app. It references `QueryKit/QueryKit.csproj` directly, so it always runs the working-tree source. It has a fixed `Recipe` model with seed data (see `features/README.md`).

One `qk run` executes one driver process. The process runs the same input against two targets:

- `memory` uses the `IEnumerable<T>` overloads on a `List<Recipe>`.
- `postgres` uses the `IQueryable<T>` overloads through EF Core and Npgsql. It returns the SQL (`ToQueryString()`) and the rows that Postgres returned.

The driver prints one JSON document with the input, the parsed LINQ expression, and one result per target. Each result holds `count`, `titles` (in result order), and `rows`, or an `error` with the exception type and message.

## Launch

The launch model is a short-lived CLI plus one private Postgres container for each run. Build the driver once for each run, then start a new driver process for each drive.

Requirements: a .NET 10 SDK and a running Docker daemon.

1. Start a run from the repo root:

   ```bash
   eval "$(.agents/skills/verify-querykit/qk up)"
   ```

2. Make sure that the output contains `qk: run '<id>' is up`. Then `QK_RUN` is set in your shell.
3. If each tool call starts a new shell, copy the printed `export QK_RUN=<id>` line into every later command.

`qk up` does these steps:

- It builds the driver into `$TMPDIR/querykit-verify/<id>/build`. The build does not touch the `bin/` and `obj/` directories of the repo.
- It starts the container `querykit-verify-<id>` from `postgres:17-alpine`, with the label `querykit-verify.run=<id>`, on a random port on 127.0.0.1.
- It waits until Postgres answers a TCP query.

The driver creates the schema and the seed rows the first time it connects. Two runs with different ids do not share containers, ports, or state. Thus several agents can verify at the same time.

After you change a file under `QueryKit/`, rebuild the driver:

```bash
.agents/skills/verify-querykit/qk rebuild
```

## Doctor

Run the doctor before the first drive. Also run it after each drive that fails in an unexpected way.

```bash
.agents/skills/verify-querykit/qk doctor
```

The doctor is read-only. Each line starts with `ok` or `FAIL`. It exits non-zero when a line fails. It checks these items:

- The container exists, is running, and has the label of this run.
- The host port maps to this container.
- Postgres answers a query.
- The driver dll exists.
- The driver is newer than every `.cs` and `.csproj` file under `QueryKit/` and `harness/Driver/`. A `FAIL` on this line means that the proof would use old code. Run `qk rebuild`.

The last line shows the git revision of the build and the number of uncommitted `QueryKit/` files. Record it with the proof.

If the doctor fails and `qk rebuild` does not correct it, run `qk down` and then `qk up` again. Do not drive an instance that the doctor rejects.

## Drive

```bash
.agents/skills/verify-querykit/qk run <label> [--filter '<f>'] [--sort '<s>'] [--config <preset>] [--target memory|postgres|both] [--aggregate] [--culture <name>]
```

- `<label>` names the evidence files. Use letters, digits, `.`, `_`, and `-` only. Use a label that tells the feature, for example `filtering-in-operator`.
- `--filter` calls `ApplyQueryKitFilter`. `--sort` calls `ApplyQueryKitSort` after the filter.
- `--aggregate` calls `ApplyQueryKit(new QueryKitData { Filters, SortOrder, Configuration })` instead.
- `--config` selects a `QueryKitConfiguration` preset. The default is `none`. Run `qk configs` to list the presets. The presets are in `harness/Driver/Configs.cs`.
- `--culture` sets the thread culture of the driver, for example `de-DE`. The default is the culture of the machine.
- `--target` defaults to `both`. Use `both` for a proof. A difference between the two targets is a finding.

Put the filter in single quotes in the shell, because QueryKit strings use double quotes:

```bash
.agents/skills/verify-querykit/qk run filtering-contains --filter 'Title @=* "salt" && Rating > 3' --sort 'Rating desc'
```

Exit codes:

| Exit | Meaning |
|---|---|
| `0` | All targets returned rows. |
| `2` | QueryKit threw a `QueryKitException` on a target. This is the expected result for an error-handling proof. |
| `1` | A usage error, an infrastructure error, or an exception that is not a `QueryKitException`. |

To see the stored data from a second view, run a read-only SQL statement:

```bash
.agents/skills/verify-querykit/qk psql 'select "Title", "Rating" from "Recipes" order by "Title"'
```

If the harness cannot express a scenario, add a preset to `harness/Driver/Configs.cs` or extend the model in `harness/Driver/Model.cs`. Then run `qk rebuild`, and update `features/README.md` for the new seed data. Do not write a unit test and call it a live proof.

The feature map in `features/` lists each feature, its sub-features, and exact commands with expected results. Read `features/README.md` first.

## Evidence

Each `qk run` writes three files to `.verify-artifacts/querykit/<run-id>/` in the repo:

- `<label>.cmd` holds the exact command and the exit code.
- `<label>.json` holds the driver output: the input, the LINQ expression, the SQL, and the rows or the error for each target.
- `<label>.stderr` holds the driver stderr. It is usually empty.

Run `qk evidence` to print the directory. The directory `.verify-artifacts/` has its own `.gitignore`, so git ignores the evidence. `qk down` never removes evidence.

Proof standards:

- Drive the public extension methods through `qk run`. Do not call internal parser types, and do not use test helpers.
- Capture the input and the result. The JSON holds both. Name the label for the feature and the case.
- For a filter, assert the `titles` of both targets against the seed data. Also read the `sql` to make sure that Postgres applied the filter in the `WHERE` clause.
- For a sort, assert the order of `titles` on both targets. The driver loads related rows in split-query mode, so EF Core adds `"Id"` as the last `ORDER BY` key. Rows with equal sort keys, or rows without a sort, come back in `Id` order, which is the seed order.
- For an error, assert the `error.type`, `isQueryKitException`, and the exit code.
- For a bug fix, capture the failure first with the old build. Then rebuild, and capture the same label with a `-fixed` suffix.
- When memory and Postgres give different results, report the difference. Do not pick one target and hide the other.

## Cleanup

```bash
.agents/skills/verify-querykit/qk down
```

`qk down` removes only the container with the label of this run and the scratch state in `$TMPDIR/querykit-verify/<id>`. It refuses to remove a container with a different label. The evidence in `.verify-artifacts/querykit/<id>/` stays.

Run `qk down` after the last drive. Also run it after each failed attempt, before you start again. To find containers of old runs, run `qk ls`. Remove an old run with `QK_RUN=<id> qk down` only if you started it. Never remove containers by image name or by process name.

## Helpers

| Command | Purpose |
|---|---|
| `qk up [--run <id>]` | Build the driver, start Postgres, print `export QK_RUN=<id>`. |
| `qk doctor` | Read-only health check of the run in `$QK_RUN`. |
| `qk rebuild` | Rebuild the driver after source changes. |
| `qk run <label> [args]` | Run one drive and save the evidence. |
| `qk psql '<sql>'` | Run a read-only SQL statement in the database of this run. |
| `qk configs` | List the `--config` presets. |
| `qk evidence` | Print the evidence directory. |
| `qk ls` | List all verification containers on this machine. |
| `qk down` | Remove the container and scratch state of this run. The evidence stays. |

## Maintenance

The feature map goes out of date when QueryKit changes. Run `/maintain-verification-skill` to audit this skill and the map against the source and a live pass. That skill edits only this directory.
