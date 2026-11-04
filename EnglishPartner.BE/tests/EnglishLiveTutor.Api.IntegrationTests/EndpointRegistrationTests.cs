using System.Text.RegularExpressions;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests;

public class EndpointRegistrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public EndpointRegistrationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void NoTwoEndpoints_ShareMethodAndRouteTemplate()
    {
        _ = _factory.CreateClient();
        var dataSources = _factory.Services.GetServices<EndpointDataSource>();
        var endpoints = dataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        var endpointKeys = new List<(string Key, string RawRoute, RouteEndpoint Endpoint)>();

        foreach (var ep in endpoints)
        {
            var rawRoute = ep.RoutePattern.RawText ?? string.Empty;
            if (!rawRoute.StartsWith('/'))
            {
                rawRoute = "/" + rawRoute;
            }

            var normalizedTemplate = NormalizeRouteTemplate(rawRoute);
            var httpMethods = ep.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;

            if (httpMethods != null && httpMethods.Count > 0)
            {
                foreach (var method in httpMethods)
                {
                    var key = $"{method.ToUpperInvariant()} {normalizedTemplate}";
                    endpointKeys.Add((key, rawRoute, ep));
                }
            }
            else
            {
                var key = $"* {normalizedTemplate}";
                endpointKeys.Add((key, rawRoute, ep));
            }
        }

        var duplicates = endpointKeys
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} -> [{string.Join(", ", g.Select(x => $"{x.RawRoute} ({x.Endpoint.DisplayName})"))}]")
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"Found duplicate endpoints with matching method and route template:\n{string.Join("\n", duplicates)}");
    }

    private static string NormalizeRouteTemplate(string route)
    {
        // Strip parameter names and constraints: {id:guid} -> {}
        var normalized = Regex.Replace(route, @"\{[^}]*\}", "{}");

        // Strip trailing slash (keeping empty string if route was root "/")
        normalized = normalized.TrimEnd('/');

        return normalized.ToLowerInvariant();
    }
}
