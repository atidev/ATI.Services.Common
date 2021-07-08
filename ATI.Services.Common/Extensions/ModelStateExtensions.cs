using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ATI.Services.Common.Extensions
{
    public static class ModelStateExtensions
    {
        public static List<string> GetErrors(this ModelStateDictionary modelState)
        {
            return modelState.Values.SelectMany(v => v.Errors)
                .Select(v => string.IsNullOrEmpty(v.ErrorMessage) ? v.Exception.Message : v.ErrorMessage).ToList();
        }
    }
}
