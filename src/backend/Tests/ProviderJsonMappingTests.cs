using System.Net;
using System.Text;
using System.Text.Json;
using OuterloopLabApi.Providers;
using Xunit;

namespace Tests;

public sealed class ProviderJsonMappingTests
{
    [Fact]
    public async Task ExtractsRate_From_RateProperty()
    {
        var handler = new StubHttpMessageHandler("{\"rate\":0.92,\"date\":\"2026-08-01\"}");
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var provider = new FrankfurterLikeRateProvider(http);

        var result = await provider.GetRateAsync("USD", "EUR");
        Assert.True(result.Success);
        Assert.Equal(0.92m, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderMarker);
    }

    [Fact]
    public async Task ExtractsRate_From_RatesObject()
    {
        var payload = "{\"rates\":{\"EUR\":0.88},\"date\":\"2026-08-01\"}";
        var handler = new StubHttpMessageHandler(payload);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var provider = new FrankfurterLikeRateProvider(http);

        var result = await provider.GetRateAsync("USD", "EUR");
        Assert.True(result.Success);
        Assert.Equal(0.88m, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderMarker);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _payload;

        public StubHttpMessageHandler(string payload)
        {
            _payload = payload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(msg);
        }
    }
}
