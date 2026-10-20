namespace Contracts.Application;

public interface IFrontendUrlBuilder
{
    string BuildUrl(string relativePath);
}
