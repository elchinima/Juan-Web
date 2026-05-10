using Microsoft.AspNetCore.Mvc.Filters;

namespace Juan_NET.Web.Services
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AdminPermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _permissionKey;

        public AdminPermissionAttribute(string permissionKey)
        {
            _permissionKey = permissionKey;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var accessService = context.HttpContext.RequestServices.GetRequiredService<AdminAccessService>();

            if (!await accessService.HasPermissionAsync(context.HttpContext.User, _permissionKey))
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
