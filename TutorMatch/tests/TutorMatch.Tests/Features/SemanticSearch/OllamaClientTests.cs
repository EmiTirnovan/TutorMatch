using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TutorMatch.Api.Features.SemanticSearch.Services;
using Xunit;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class OllamaClientTests
{
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    [Fact]
    public async Task Given_ValidResponse_When_CallingGenerateEmbedding_Then_ReturnsFloatArray()
    {
        // Arrange
        float[] expectedEmbedding = [0.12f, 0.34f, -0.56f, 0.78f];
        HttpRequestMessage? capturedRequest = null;

        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            var json = JsonSerializer.Serialize(new
            {
                model = "bge-m3",
                embeddings = new[] { expectedEmbedding }
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var factory = new TestHttpClientFactory(httpClient);
        var configuration = new ConfigurationBuilder().Build();

        var client = new OllamaClient(factory, configuration);

        // Act
        var result = await client.GenerateEmbeddingAsync("Algoritmi si POO");

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(expectedEmbedding);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/api/embed");
    }

    [Fact]
    public async Task Given_HttpErrorStatus_When_CallingGenerateEmbedding_Then_ReturnsNullWithoutThrowing()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);
        var factory = new TestHttpClientFactory(httpClient);
        var configuration = new ConfigurationBuilder().Build();

        var client = new OllamaClient(factory, configuration);

        // Act
        var result = await client.GenerateEmbeddingAsync("Test input");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_EmptyOrWhitespaceText_When_CallingGenerateEmbedding_Then_ReturnsNullImmediately()
    {
        // Arrange
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler(_ =>
        {
            httpCalled = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var factory = new TestHttpClientFactory(httpClient);
        var configuration = new ConfigurationBuilder().Build();

        var client = new OllamaClient(factory, configuration);

        // Act
        var resultNull = await client.GenerateEmbeddingAsync(null!);
        var resultEmpty = await client.GenerateEmbeddingAsync("   ");

        // Assert
        resultNull.Should().BeNull();
        resultEmpty.Should().BeNull();
        httpCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Given_MalformedJsonResponse_When_CallingGenerateEmbedding_Then_ReturnsNullWithoutThrowing()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ not valid json }", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler);
        var factory = new TestHttpClientFactory(httpClient);
        var configuration = new ConfigurationBuilder().Build();

        var client = new OllamaClient(factory, configuration);

        // Act
        var result = await client.GenerateEmbeddingAsync("Valid query");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_CustomConfiguration_When_Constructed_Then_UsesConfiguredValues()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Ollama:BaseUrl"] = "http://ai-server:9999",
            ["Ollama:Model"] = "nomic-embed-text",
            ["Ollama:TimeoutSeconds"] = "3"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            capturedBody = req.Content?.ReadAsStringAsync().Result;
            var json = JsonSerializer.Serialize(new
            {
                model = "nomic-embed-text",
                embeddings = new[] { new[] { 1.0f, 2.0f } }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var factory = new TestHttpClientFactory(httpClient);

        var client = new OllamaClient(factory, configuration);

        // Act
        var result = await client.GenerateEmbeddingAsync("Physics tutor");

        // Assert
        result.Should().NotBeNull();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("http://ai-server:9999/api/embed");
        capturedBody.Should().Contain("\"model\":\"nomic-embed-text\"");
    }
}

