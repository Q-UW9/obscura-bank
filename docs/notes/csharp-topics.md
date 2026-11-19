# C# topics covered

One line per topic, for revision. Where each one first came up is in brackets.

## Language

- **Top-level statements:** the compiler generates `Main` and `class Program`; only one file per project may use them. [Program.cs]
- **`var`:** local type inference; the type is still static, like Java 10's `var`. [Program.cs]
- **`string` vs `String`:** the same type in C# (`string` is an alias), unlike TypeScript, where `String` is the wrapper object. [App.tsx lint error]
- **`??` and `throw` expressions:** `x ?? throw new …()` fails fast when a value is null. [Program.cs]
- **Nullable reference types (`<Nullable>enable`):** the compiler warns about possible null use, like TypeScript's `strictNullChecks`. [csproj]
- **Implicit usings:** common namespaces are imported automatically; `using X;` is C#'s `import`. [csproj]
- **File-scoped namespace:** `namespace A.B;` applies to the whole file; like a Java package, but folders don't have to match. [MigrationRunner]
- **Static class:** can't be instantiated and holds only static members; fine when there's no state, but it can't be injected or faked. [MigrationRunner]
- **Anonymous types:** `new { status = "ok" }`, a throwaway object; serialized to JSON or used as Dapper parameters. [Program.cs, MigrationRunner]
- **Raw string literals:** `"""…"""`, multi-line with no escaping; the indentation of the closing quotes is removed. [MigrationRunner]
- **String interpolation:** `$"…{x}…"`; not used in logger calls (structured logging). [MigrationRunner]
- **Named arguments:** `ExecuteAsync(sql, transaction: tx)` picks a parameter by name and skips optional ones. [MigrationRunner]
- **Method groups:** `OrderBy(Path.GetFileName)` passes the method itself, uncalled, as a delegate. [MigrationRunner]
- **Extension methods:** Dapper's `ExecuteAsync`/`QueryAsync` and LINQ add methods to existing types; they become visible through `using`. [Program.cs]
- **LINQ:** `Where`/`Select`/`OrderBy`/`ToHashSet` on any `IEnumerable<T>`; lazy until enumerated, like Java streams. [MigrationRunner]
- **`StringComparer.Ordinal`:** compares raw character codes; the default comparison depends on the system's culture (locale). [MigrationRunner]
- **`async`/`await` and `Task`:** `await` frees the thread while waiting; avoid `async void` and `.Result`/`.Wait()`. [Program.cs, MigrationRunner]
- **`IDisposable`/`IAsyncDisposable` and `await using`:** dispose at the end of scope, like Java's try-with-resources; disposing a connection returns it to the pool, disposing an uncommitted transaction rolls it back. [Program.cs, MigrationRunner]
- **`try`/`finally`:** cleanup that must run even when an exception occurs (advisory unlock). [MigrationRunner]
- **`const`:** a compile-time constant, like Java's `static final` for primitives. [MigrationRunner]

## ASP.NET Core

- **`WebApplication` builder, two phases:** register services before `Build()`; middleware and routes after it. [Program.cs]
- **Configuration layering:** `appsettings.json` → `appsettings.{Environment}.json` → environment variables. [appsettings.Development.json]
- **Minimal API handlers:** the lambda's return value is the response; strings become text, objects become JSON. [Program.cs]
- **Parameter injection:** handler parameters are filled from DI, the route, the query or the body. [Program.cs]
- **`AddSingleton`:** one instance for the app's lifetime (`NpgsqlDataSource` owns the connection pool). [Program.cs]
- **Developer Exception Page:** stack traces only in Development; a security concern if it ever ran in Production. [health check 500]
- **Structured logging:** message templates with `{Name}` placeholders store values as separate fields. [MigrationRunner]

## Tooling

- **`.csproj` / `.slnx` / NuGet / `dotnet` CLI:** project file, solution, package registry, all-in-one CLI. [setup]
- **MSBuild items:** `<None Include Link CopyToOutputDirectory>` copies files next to the `.dll`. [csproj]
