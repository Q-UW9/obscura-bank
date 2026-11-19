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
- **`static readonly`:** set once at first use and never again; for objects, where `const` isn't allowed. [Argon2Parameters]
- **Positional `record`:** generates read-only properties, a constructor and `ToString`; **value equality** (`==` compares contents). [Argon2Parameters]
- **`sealed`:** no inheritance, like Java's `final class`. [Argon2Parameters]
- **Target-typed `new`:** `Argon2Parameters x = new(…)` when the type is already known. [Argon2Parameters]
- **`readonly` fields and the `_name` convention:** assignable only in the constructor; private fields start with `_`. Primary constructors (`class X(Dep d)`) are the shorter C# 12 alternative. [PasswordHasher]
- **Object initializers:** `new T(…) { Prop = value, }` sets properties right after construction. [PasswordHasher]
- **Expression-bodied members:** `int F() => expr;` for one-expression methods. [PasswordHasher]
- **`$` inside an interpolated string is literal:** only `{…}` interpolates, unlike JavaScript's `${…}`. [PasswordHasher]
- **`RandomNumberGenerator` vs `Random`:** the first is cryptographically secure (salts, tokens), the second is predictable and only for games and simulations. [PasswordHasher]
- **Strings are immutable; `Normalize(NormalizationForm.FormKC)`:** returns a *new* string in Unicode NFKC form, like Java's `Normalizer.normalize`. [PasswordHasher]
- **`out` parameters and the `TryParse` pattern:** `int.TryParse(s, out var n)` returns success as `bool` and the value through `out`; no exception for expected bad input. Java has no `out`. [PasswordHasher]
- **Culture-sensitive parsing:** parse machine formats with `CultureInfo.InvariantCulture` (and strict `NumberStyles`); the default uses the system locale (German: `1,5`). [PasswordHasher]
- **Range operator:** `s[3..]` takes a substring from index 3, like JavaScript's `slice(3)`. [PasswordHasher]
- **Nested private types:** a `private record` inside a class, visible only there. [PasswordHasher]
- **Exceptions vs. return values:** return `false` for expected outcomes (wrong password), throw for broken invariants (corrupted stored hash). [PasswordHasher]
- **Guard clauses with throw helpers:** `ArgumentException.ThrowIfNullOrEmpty(x)` (and `ArgumentNullException.ThrowIfNull`) reject bad arguments in one line and name the parameter automatically. [PasswordHasher]
- **`CryptographicOperations.FixedTimeEquals`:** compares byte arrays without an early exit; returns `false` immediately only if the lengths differ. [PasswordHasher]
- **Strings are UTF-16; `string.Length` counts code units, not characters:** an emoji outside the BMP is 2 units; `EnumerateRunes()` yields `Rune`s (Unicode scalar values, i.e. code points). Same pitfall as Java's `String.length()`. [PasswordPolicy]
- **Relational and logical patterns:** `x is >= 15 and <= 128` is a range check through pattern matching (C# 9). [PasswordPolicy]
- **`T?` for value types vs reference types:** `long?` is `Nullable<long>`, a real type with `.HasValue`/`.Value` (Java: boxed `Long`); `string?` is only a compiler annotation for nullable reference types. [UserRepository]
- **Exception filters:** `catch (PostgresException e) when (…)` catches only matching exceptions; others propagate untouched, and the stack isn't unwound for them. [UserRepository]
- **Dapper constructor mapping:** a positional record is filled through its constructor; column names must match parameter names (case-insensitive), hence `password_hash AS PasswordHash`. [UserRepository]
- **CPU-bound vs I/O-bound:** `async` helps while *waiting* (DB, network), not while *computing*, so Argon2id is called synchronously. [PasswordHasher]

## ASP.NET Core

- **`WebApplication` builder, two phases:** register services before `Build()`; middleware and routes after it. [Program.cs]
- **Configuration layering:** `appsettings.json` → `appsettings.{Environment}.json` → environment variables. [appsettings.Development.json]
- **Minimal API handlers:** the lambda's return value is the response; strings become text, objects become JSON. [Program.cs]
- **Parameter injection:** handler parameters are filled from DI, the route, the query or the body. [Program.cs]
- **`AddSingleton`:** one instance for the app's lifetime (`NpgsqlDataSource` owns the connection pool). [Program.cs]
- **`app.Environment.IsDevelopment()`:** reads `ASPNETCORE_ENVIRONMENT` (set in `launchSettings.json`); gates dev-only behaviour such as startup migrations. [Program.cs]
- **`app.Services.GetRequiredService<T>()`:** asks the built DI container directly (service locator); fine at startup, but handlers should receive dependencies as parameters. It throws if `T` isn't registered, while `GetService<T>()` returns null. [Program.cs]
- **`AppContext.BaseDirectory`:** the folder of the running `.dll`, independent of the current working directory. [Program.cs]
- **Top-level `await`:** allowed in top-level statements; the generated `Main` becomes `async Task Main`. [Program.cs]
- **DI lifetimes:** singleton (one per app, must be thread-safe), scoped (one per HTTP request), transient (new each time it's resolved). [Program.cs, PasswordHasher]
- **Captive dependency:** a singleton holding a scoped service keeps it alive forever and shares it across requests; ASP.NET Core rejects this in Development (`ValidateScopes`). [Program.cs]
- **`AddSingleton(instance)` vs `AddSingleton<T>()`:** the first registers an object you built; the second lets the container build `T` and inject its constructor parameters (constructor injection). [Program.cs]
- **`ValidateOnBuild`:** in Development, `Build()` checks that every registered service can be constructed, so a missing dependency fails at startup. [Program.cs]
- **Developer Exception Page:** stack traces only in Development; a security concern if it ever ran in Production. [health check 500]
- **Structured logging:** message templates with `{Name}` placeholders store values as separate fields. [MigrationRunner]

## Testing (xUnit)

- **`[Fact]` vs `[Theory]` + `[InlineData]`:** a single test vs. one test method run once per data row. [PasswordHasherTests]
- **A new test-class instance per test:** the constructor and field initializers are the setup (no `[SetUp]`); `IDisposable.Dispose` is the teardown. [PasswordHasherTests]
- **`Assert.Throws<T>(() => …)`:** passes a lambda so xUnit can call the code and catch the exception itself. [PasswordHasherTests]
- **Unicode escapes:** `"é"` is one precomposed character; `"é"` is `e` plus a combining accent; `"\U0001F512"` (capital U, 8 digits) is a code point outside the BMP, such as an emoji. [PasswordHasherTests, PasswordPolicyTests]

## Tooling

- **`.csproj` / `.slnx` / NuGet / `dotnet` CLI:** project file, solution, package registry, all-in-one CLI. [setup]
- **MSBuild items:** `<None Include Link CopyToOutputDirectory>` copies files next to the `.dll`. [csproj]
