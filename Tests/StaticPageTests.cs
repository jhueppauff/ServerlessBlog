using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Moq;
using ServerlessBlog.Frontend;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Azure.Data.Tables;
using Azure;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task StaticPageFunctionShouldReturnProperHTLM()
    {
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization());

        var httpRequest = fixture.Freeze<HttpRequest>();
        var logger = fixture.Freeze<ILogger>();

        string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\..\\..\\..\\..\\Frontend\\statics";
        Microsoft.Azure.WebJobs.ExecutionContext executionContext = fixture.Freeze<Microsoft.Azure.WebJobs.ExecutionContext>();
        executionContext.FunctionDirectory = path;

        var tableClient = fixture.Create<TableClient>();

        var pageFunction = fixture.Create<StaticPageFunctions>();

        var result = await pageFunction.IndexPage(httpRequest, logger, executionContext) as ObjectResult;

        result.StatusCode.Should().Be(200);
        result.ContentTypes.First().Should().Be("text/html");
        result.Value.Should().NotBeNull();
    }
}