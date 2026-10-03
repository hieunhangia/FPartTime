using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Frontend.Extensions;

public static class TempDataExtensions
{
    extension(ITempDataDictionary tempData)
    {
        public void SetInfoMessage(string message) => tempData["InfoMessage"] = message;
        public void SetSuccessMessage(string message) => tempData["SuccessMessage"] = message;
        public void SetWarningMessage(string message) => tempData["WarningMessage"] = message;
        public void SetErrorMessage(string message) => tempData["ErrorMessage"] = message;
    }
}