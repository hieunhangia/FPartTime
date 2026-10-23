namespace Frontend.ViewModels;

public class OpenForJobBannerViewModel
{
    public bool IsOpenForJob { get; set; }
    public bool CanRefresh { get; set; }
    public DateTimeOffset? OpenForJobUntil { get; set; }
    public string? ReturnUrl { get; set; }
}
