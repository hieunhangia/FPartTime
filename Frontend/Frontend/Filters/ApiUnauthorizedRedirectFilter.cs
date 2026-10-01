using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Filters;

public class ApiUnauthorizedRedirectFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var executedContext = await next();
        if (executedContext is { Exception: ApiException { ResponseStatusCode: 401 }, ExceptionHandled: false })
        {
            executedContext.ExceptionHandled = true;
            context.Result = new RedirectToPageResult("/Identity/LoginOrRegister");
        }
    }
}