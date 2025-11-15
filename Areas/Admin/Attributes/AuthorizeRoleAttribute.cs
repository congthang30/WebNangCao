using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Services.AUTH;

namespace NewWeb.Areas.Admin.Attributes
{
    public class AuthorizeRoleAttribute : Attribute, IAsyncPageFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            
            if (!await authService.IsAuthenticatedAsync())
            {
                context.Result = new RedirectToPageResult("/Account/Login");
                return;
            }

            var userRole = await authService.GetCurrentUserRoleAsync();
            
            if (userRole == null || !_allowedRoles.Contains(userRole))
            {
                context.Result = new RedirectToPageResult("/Account/Login");
                return;
            }

            await next();
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}

