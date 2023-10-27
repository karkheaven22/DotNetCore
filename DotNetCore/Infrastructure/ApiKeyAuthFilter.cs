using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCore
{
    public class ApiKeyAttribute : ServiceFilterAttribute
    {
        public ApiKeyAttribute() : base(typeof(ApiKeyAuthFilter)) { }
    }

    public class ApiKeyAuthFilter : IAuthorizationFilter
    {
        private readonly IApiKeyValidation _apiKeyValidation;

        public ApiKeyAuthFilter(IApiKeyValidation apiKeyValidation)
        {
            _apiKeyValidation = apiKeyValidation;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string userApiKey = context.HttpContext.Request.Headers[Constants.ApiKeyHeaderName].ToString();

            if (string.IsNullOrWhiteSpace(userApiKey))
            {
                context.Result = new BadRequestResult();
                return;
            }

            if (!_apiKeyValidation.IsValidApiKey(userApiKey))
                context.Result = new UnauthorizedResult();
        }
    }
}
