using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Text.RegularExpressions;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task HomepageHasTitle()
    {
        await Page.GotoAsync("https://blog.hueppauff.com");

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Blog - Julian Hueppauff.com"));
    }

    [Test]
    public async Task BlogNavigationTest()
    {
        await Page.GotoAsync("https://blog.hueppauff.com/");

        await Page.GetByText("Share your Bicep Modules using the Bicep Registry 31.03.2023 Using the Azure Con").ClickAsync();

        await Page.Locator(".btn").First.ClickAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Julian Hüppauff" }).ClickAsync();

        await Page.Locator("div:nth-child(3) > .card-body > .btn").ClickAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "License" }).ClickAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Home (current)" }).ClickAsync();

        await Page.Locator("div:nth-child(14) > .card-body > .btn").ClickAsync();

        await Page.GotoAsync("https://blog.hueppauff.com/");
    }

    [Test]
    public async Task BlogPostElementsAreOnPage()
    {
        await Page.GotoAsync("https://blog.hueppauff.com");

        var locator = Page.Locator("");

        // Check if the blog post title is on the page
        var posts = await Page.QuerySelectorAllAsync("div.card.mb-4.shadow-lg");
        
        //Assert.NotNull(postTitle);

        // Check if the blog post date is on the page
        var postDate = await Page.QuerySelectorAsync("p.card-text:nth-child(2)");
        Assert.NotNull(postDate);

        // Check if the blog post content is on the page
        var postContent = await Page.QuerySelectorAsync("p.card-text:nth-child(3)");
        Assert.NotNull(postContent);

        // Check if the blog post tags are on the page
        var postTags = await Page.QuerySelectorAsync(".tags");
        Assert.NotNull(postTags);
    }
}