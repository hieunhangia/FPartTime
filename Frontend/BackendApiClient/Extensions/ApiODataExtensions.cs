using Microsoft.Kiota.Abstractions;
using OData.QueryBuilder.Builders;
using OData.QueryBuilder.Conventions.AddressingEntities.Query;

namespace BackendApiClient.Extensions;

public static class ApiODataExtensions
{
    public static async Task<List<TResponse>?> GetWithOdataAsync<TResponse>(this BaseRequestBuilder requestBuilder,
        Action<IODataQueryCollection<TResponse>> configureQuery)
    {
        dynamic builder = requestBuilder;
        RequestInformation requestInfo = builder.ToGetRequestInformation();
        var odataBuilder = new ODataQueryBuilder(requestInfo.URI).For<TResponse>(string.Empty).ByList();
        configureQuery(odataBuilder);
        return await builder.WithUrl(odataBuilder.ToUri().ToString()).GetAsync();
    }
}