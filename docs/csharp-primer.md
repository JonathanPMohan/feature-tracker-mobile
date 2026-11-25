# C# primer for a TypeScript developer

This covers what M1 to M5 need, each idea shown next to the TypeScript you already know. It's not a full tour; later specs add topics as they come up. Real examples are in the repo: `PageParam.cs`, `PageParamTests.cs` and `Program.cs`.

**The big difference first:** TypeScript's types are erased before the code runs, while C#'s types exist at runtime and the compiler enforces them strictly. There's no `any` escape hatch in everyday code, and a type error always stops the build.

---

## 1. Types and `var`

```ts
// TypeScript
const title: string = "Dark mode";
let votes = 3;            // inferred: number
const ratio = 0.5;        // number (there's only one number type)
```

```csharp
// C#
string title = "Dark mode";
var votes = 3;            // inferred: int. `var` is still strongly typed.
var ratio = 0.5;          // double
```

- **Numbers come in sizes:** `int` (whole, about ±2.1 billion), `long` (bigger whole numbers), `double` (decimal fractions), and `decimal` (exact, for money). Postgres `integer` maps to `int`.
- **`var` isn't `let`.** It means "the compiler infers the type here", and the type never changes. `var votes = 3; votes = "x";` is a compile error.
- **Nothing is `const` by default**, so a local variable can be reassigned. `const` exists only for compile-time constants like `PageParam.FirstPage`. Read-only data is usually expressed with records (section 2) instead.
- **Strings use double quotes.** Interpolation is `$"Hello {name}"`, where TypeScript writes `` `Hello ${name}` ``.
- **Names:** types, methods and properties are `PascalCase`. Local variables and parameters are `camelCase`. Private fields are `_camelCase` (enforced by `.editorconfig`).

## 2. `class` vs `record`

```ts
// TypeScript: a data shape
type ApiInfo = { readonly name: string };
const info: ApiInfo = { name: "Feature Tracker API" };
```

```csharp
// C#: a record (one line declares the type, its property and a constructor)
public record ApiInfo(string Name);
var info = new ApiInfo("Feature Tracker API");
```

| Use a… | When | Equality |
| --- | --- | --- |
| **`record`** | Data that's passed around: API responses and request bodies (DTOs) | By value: two `ApiInfo("x")` are equal |
| **`class`** | Things with identity or behavior: EF Core entities (M4), services | By reference: two separate objects are never equal, even with the same values |

Objects normally come from a declared type with `new`. C# has anonymous objects (`new { name = "x" }`), but they can't be passed around with a type name, so this project uses records instead. "Copy with one change" uses `with`, which TypeScript does with a spread:

```csharp
var renamed = info with { Name = "New name" };   // ≈ { ...info, name: "New name" }
```

A class with properties looks like this (you'll write entities like it in M4):

```csharp
public class FeatureRequest
{
    public int Id { get; set; }                    // { get; set; } = a property: readable and writable
    public string Title { get; set; } = "";        // initialized so it's never null
    public DateTimeOffset CreatedAt { get; set; }
}
```

**Access modifiers:** `public` is visible everywhere. `internal` is visible only inside this project, so the tests can't see it unless we allow them. `private` is visible only inside the class. TypeScript has nothing like `internal`; it's closest to "not exported from the package".

## 3. Nullable: `?`, `!` and `??`

With `<Nullable>enable</Nullable>` (`Directory.Build.props`), this works like TypeScript's `strictNullChecks`:

| TypeScript | C# | Meaning |
| --- | --- | --- |
| `string` | `string` | never null |
| `string \| null` | `string?` | may be null |
| `user?.name` | `user?.Name` | null if `user` is null |
| `value ?? "default"` | `value ?? "default"` | fallback when null |
| `if (user === null)` | `if (user is null)` | null check (use `is null`, not `== null`) |
| `user!.name` | `user!.Name` | "trust me, it's not null". Avoid; the compiler can't check it |

```csharp
public static string Greeting(string? name)
{
    if (name is null)
    {
        return "Hello, visitor";
    }
    // After the check, the compiler knows `name` is a non-null string here,
    // just like TypeScript's narrowing.
    return $"Hello, {name}";
}
```

Because warnings are errors, forgetting a null check **fails the build**. That's how M5 to M8 will make "a visitor (`null` user) is refused" impossible to forget.

## 4. `async Task<T>`

```ts
// TypeScript
async function getRequest(id: number): Promise<FeatureRequest | null> {
  const row = await db.query.featureRequest.findFirst({ where: eq(t.id, id) });
  return row ?? null;
}
```

```csharp
// C#
public async Task<FeatureRequest?> GetRequestAsync(int id)
{
    return await _db.FeatureRequests.FirstOrDefaultAsync(r => r.Id == id);
}
```

- **`Task<T>` is `Promise<T>`.** A plain `Task` is `Promise<void>`.
- **Async methods end in `Async`** by convention: `SaveChangesAsync`, `FirstOrDefaultAsync`.
- **Always `await` a Task.** A forgotten `await` is the same bug as a forgotten `await` in JavaScript, but here the compiler warns you, and with warnings as errors, the build stops.
- **`r => r.Id == id`** is a lambda: an arrow function, with `==` doing the job of `===`. C# has no loose equality to worry about.

## 5. LINQ: `.filter`/`.map` with different names

LINQ ("language-integrated query") is the set of collection methods. EF Core translates the **same methods** into SQL, which is how you'll write queries.

| TypeScript array | C# LINQ |
| --- | --- |
| `.filter(x => …)` | `.Where(x => …)` |
| `.map(x => …)` | `.Select(x => …)` |
| `.find(x => …)` | `.FirstOrDefault(x => …)` |
| `.some(x => …)` | `.Any(x => …)` |
| `.length` | `.Count()` |
| `.sort((a, b) => b.votes - a.votes)` | `.OrderByDescending(x => x.Votes).ThenByDescending(x => x.CreatedAt)` |
| `.slice(20, 40)` | `.Skip(20).Take(20)` |

```csharp
// In memory (a List<T>), it runs like JavaScript array methods.
var titles = requests
    .Where(r => r.Status == "open")
    .OrderByDescending(r => r.CreatedAt)
    .Select(r => r.Title)
    .ToList();

// On an EF Core DbSet, the SAME code becomes one SQL query
// (WHERE … ORDER BY … SELECT title), sent when you call ToListAsync().
var titles = await _db.FeatureRequests
    .Where(r => r.Status == "open")
    .OrderByDescending(r => r.CreatedAt)
    .Select(r => r.Title)
    .ToListAsync();
```

**The Drizzle comparison:** Drizzle's query builder looked like SQL. LINQ looks like array methods, but against the database it **is** SQL. In M2 we'll turn on query logging so you can see the SQL that each LINQ query becomes.

## 6. Attributes: `[Fact]`, `[Theory]`

Square brackets above a method or class attach information that a framework reads. Unlike a comment, they change behavior.

```csharp
[Theory]                       // xUnit: "this is a test, run once per row"
[InlineData("2", 2)]           // a row of arguments
public void Valid_page_numbers_are_returned(string value, int expected) { … }
```

The closest TypeScript equivalents are decorators, or Vitest's `it.each`. Attributes you'll meet later include `[Fact]` (a single test) and, if your team uses Controllers, `[HttpGet]` and `[Authorize]`.

## 7. Dependency injection: how `Program.cs` hands out services

In Next.js you imported what you needed: `import { db } from "@/db"`. Every file shared that one module-level `db`.

In ASP.NET Core you **register** services once in `Program.cs`, and the framework **passes** them to whatever asks for them:

```csharp
// Program.cs, phase 1: register (M2 does this for the database)
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Phase 2: an endpoint asks for one just by listing it as a parameter.
// ASP.NET Core sees the AppDbContext parameter and supplies one.
app.MapGet("/requests/{id}", async (int id, AppDbContext db) =>
    await db.FeatureRequests.FindAsync(id));
```

A class gets its services through its **constructor**:

```csharp
public class VoteService(AppDbContext db)        // a "primary constructor"
{
    public Task<int> CountAsync(int requestId) =>
        db.Votes.CountAsync(v => v.RequestId == requestId);
}
```

**Why bother, compared with `import { db }`?**
- **Lifetime:** the framework creates a fresh `AppDbContext` for each HTTP request and disposes of it afterwards. You never manage connections yourself.
- **Tests can swap things:** in M2, the integration tests replace the real database connection with a Testcontainers one in a single place, without touching any endpoint.

The three lifetimes you'll see: **Singleton** (one instance for the whole app), **Scoped** (one per HTTP request, which is what `AppDbContext` uses), and **Transient** (a new one every time it's asked for).

---

## Quick reference

| TypeScript | C# |
| --- | --- |
| `import { x } from "./y"` | `using Namespace.Of.Y;` (whole namespaces, not single names) |
| `export` | `public` |
| `function f(a: string): number` | `int F(string a)`: the return type comes first |
| `(a) => a + 1` | `a => a + 1` |
| `===` / `!==` | `==` / `!=` |
| `throw new Error("x")` | `throw new InvalidOperationException("x")` (specific exception types) |
| `try { } catch (e) { }` | `try { } catch (InvalidOperationException e) { }` |
| `console.log` | `logger.LogInformation("…")` (through dependency injection; see section 7) |
| `undefined` | doesn't exist; only `null` |
| `npm` / `package.json` | NuGet / `.csproj` |
