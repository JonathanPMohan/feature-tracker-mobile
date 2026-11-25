// `using` is C#'s import. This namespace isn't one of the implicit usings.
using System.Globalization;

// File-scoped namespace: everything in this file lives in
// FeatureTracker.Api.Paging. Namespaces group code like folders do, and by
// convention they match the folder path. Other files import it with
// `using FeatureTracker.Api.Paging;`.
namespace FeatureTracker.Api.Paging;

/// <summary>
/// Turns the <c>?page=</c> query value into a page number. The C# port of
/// <c>parsePageParam</c> from the Next.js tracker (rule S3): anything that
/// isn't a whole number of 1 or more means page 1, never an error.
/// </summary>
/// <remarks>
/// A <c>static class</c> can't be instantiated: it's just a home for
/// functions. It's the closest thing C# has to a TypeScript module that
/// exports plain functions, because in C# every function must live in a type.
/// </remarks>
public static class PageParam
{
    // `const` is a compile-time constant, like `export const FIRST_PAGE = 1`.
    // C# names constants in PascalCase, not SCREAMING_CASE.
    public const int FirstPage = 1;

    /// <summary>
    /// Returns the page number in <paramref name="value"/>, or
    /// <see cref="FirstPage"/> if it isn't a valid page.
    /// </summary>
    // `string?`: the value may be null (no ?page= in the URL at all).
    // Without the `?`, the compiler would refuse a null argument.
    public static int Parse(string? value)
    {
        // int.TryParse is C#'s safe parse. It never throws. It returns
        // true/false, and puts the number in `page` through an `out`
        // parameter: a second return value, written by the method.
        // (TypeScript would return `number | null` instead.)
        //
        // NumberStyles.None accepts digits only: no spaces, no + or -, no
        // decimal point, no thousands separators, no "1e3". Parsing is
        // stricter than Number("...") in JavaScript, which accepts " 2 ".
        //
        // CultureInfo.InvariantCulture means "don't use the Mac's region
        // settings". Number parsing in .NET follows the user's locale by
        // default, so always pass a culture for values from URLs.
        //
        // A number too big for `int` (over 2,147,483,647) also returns false,
        // so it can never reach the database as an overflow error.
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var page)
            && page >= FirstPage)
        {
            return page;
        }

        return FirstPage;
    }
}
