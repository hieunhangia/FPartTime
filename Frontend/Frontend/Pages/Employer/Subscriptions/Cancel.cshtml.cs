using Frontend.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages.Employer.Subscriptions;

[Authorize(Roles = Role.Employer)]
public class CancelModel : PageModel
{
    public void OnGet()
    {
    }
}
