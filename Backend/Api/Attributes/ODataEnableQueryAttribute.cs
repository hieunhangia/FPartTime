using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Primitives;
using Service;

namespace Api.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ODataEnableQueryAttribute(OutputType outputType) : EnableQueryAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var queryDict = new Dictionary<string, StringValues>(request.Query, StringComparer.OrdinalIgnoreCase);
        switch (outputType)
        {
            case OutputType.List:
                queryDict.Remove("$count");
                break;
            case OutputType.Paged:
                queryDict.TryAdd("$count", "true");
                break;
        }

        request.Query = new QueryCollection(queryDict);
        request.QueryString = QueryString.Create(queryDict.SelectMany(kv =>
            kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v))));
        base.OnActionExecuting(context);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        if (outputType != OutputType.Paged ||
            context.Result is not ObjectResult { Value: IEnumerable<object> items }) return;
        var listItems = items.ToList();
        context.Result = new ObjectResult(new PagedResponseDto<object>
        {
            TotalCount = context.HttpContext.ODataFeature().TotalCount ?? listItems.Count,
            Items = listItems
        });
    }
}

public enum OutputType
{
    List,
    Paged
}