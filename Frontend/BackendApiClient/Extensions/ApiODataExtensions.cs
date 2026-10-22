using Microsoft.Kiota.Abstractions;
using OData.QueryBuilder.Builders;
using OData.QueryBuilder.Conventions.AddressingEntities.Query;

namespace BackendApiClient.Extensions;

public static class ApiODataExtensions
{
    extension(BaseRequestBuilder requestBuilder)
    {
        public async Task<List<TResponse>?> GetWithODataAsync<TResponse>(Action<IODataQueryCollection<TResponse>> configureQuery)
        {
            dynamic builder = requestBuilder;
            RequestInformation requestInfo = builder.ToGetRequestInformation();
            var odataBuilder = new ODataQueryBuilder(requestInfo.URI).For<TResponse>(string.Empty).ByList();
            configureQuery(odataBuilder);
            return await builder.WithUrl(odataBuilder.ToUri().ToString()).GetAsync();
        }

        public async Task<TResponse?> GetPagedWithODataAsync<TEntity, TResponse>(
            Action<IODataQueryCollection<TEntity>> configureQuery,
            string? additionalFilter = null)
        {
            dynamic builder = requestBuilder;
            RequestInformation requestInfo = builder.ToGetRequestInformation();
            var odataBuilder = new ODataQueryBuilder(requestInfo.URI).For<TEntity>(string.Empty).ByList();
            configureQuery(odataBuilder);
            var uri = odataBuilder.ToUri().ToString();
            if (!string.IsNullOrWhiteSpace(additionalFilter))
            {
                if (uri.Contains("$filter="))
                {
                    uri = uri.Replace("$filter=", $"$filter={additionalFilter} and ");
                }
                else
                {
                    var sep = uri.Contains('?') ? "&" : "?";
                    uri += $"{sep}$filter={additionalFilter}";
                }
            }
            return await builder.WithUrl(uri).GetAsync();
        }
    }
}