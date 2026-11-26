// In its own file so it can have a namespace: a file with top-level statements
// (Program.cs) can't declare one, and CLAUDE.md says namespaces match folders.
namespace FeatureTracker.Api;

/// <summary>The response of <c>GET /</c>.</summary>
/// <remarks>
/// A <c>record</c> is a class made for holding data: this one line declares
/// a type with a read-only <c>Name</c> property and a constructor, and two
/// records with the same values are equal. The TypeScript equivalent is
/// <c>type ApiInfo = { readonly name: string }</c>.
/// </remarks>
internal sealed record ApiInfo(string Name);
