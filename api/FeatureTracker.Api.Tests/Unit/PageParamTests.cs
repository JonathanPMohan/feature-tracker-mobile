using FeatureTracker.Api.Paging;

namespace FeatureTracker.Api.Tests.Unit;

// A test class is a plain class. xUnit finds every public method marked
// [Fact] or [Theory] in it. There's no describe() block: the class name plays
// that role, and test names show up as "PageParamTests.Method_name".
//
// Unit tests (this folder) never touch a database, so the pre-commit hook can
// run them in a second or two. Integration tests come in M2.
public class PageParamTests
{
    // [Theory] + [InlineData]: one test method, run once per row, with the
    // row's values passed in as arguments. This is Vitest's
    // `it.each([["1", 1], ["2", 2]])`. Each row shows up as its own test
    // in the results, so a failure names the exact input.
    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("42", 43)]
    // Leading zeros are still a valid number. Harmless, so allowed.
    [InlineData("007", 7)]
    public void Valid_page_numbers_are_returned(string value, int expected)
    {
        // Arrange-Act-Assert, the usual test shape. Here "arrange" is the
        // input from [InlineData], so only two steps are left.
        var page = PageParam.Parse(value);

        // Assert.Equal(expected, actual): expected first. If you swap them,
        // the test still works, but the failure message reads backwards.
        Assert.Equal(expected, page);
    }

    // Every way the old tracker's S3 rule says "means page 1", plus the
    // ones a .NET parser could get wrong. Each comment says why it's here.
    [Theory]
    [InlineData(null)]          // no ?page= in the URL at all
    [InlineData("")]            // ?page=
    [InlineData("0")]           // S3: zero isn't a page
    [InlineData("-1")]          // S3: negative
    [InlineData("abc")]         // S3: not a number
    [InlineData("2.5")]         // not a whole number
    [InlineData(" 2")]          // whitespace: NumberStyles.None refuses it
    [InlineData("+2")]          // a sign: refused too
    [InlineData("1e3")]         // JavaScript's Number("1e3") would say 1000
    [InlineData("2,000")]       // a thousands separator
    [InlineData("99999999999")] // too big for int: refused, not an overflow
    [InlineData("٣")]           // Arabic-Indic digit 3: only ASCII 0–9 count
    public void Anything_else_means_the_first_page(string? value)
    {
        Assert.Equal(PageParam.FirstPage, PageParam.Parse(value));
    }

    // [Fact]: a single test with no parameters. Use it when there's one case,
    // and [Theory] when the same check runs over many inputs.
    [Fact]
    public void The_first_page_is_page_1()
    {
        // Guards the constant itself: if someone changes FirstPage to 0
        // (zero-based paging), this fails and makes them think about S3.
        Assert.Equal(1, PageParam.FirstPage);
    }
}
