using BackendApiClient.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[Route("[controller]/[action]")]
public class IdentityController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutApiTokenAsync();
        return Redirect("/");
    }
}