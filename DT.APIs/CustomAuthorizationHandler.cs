using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

public class CustomAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        // Check if the user is authenticated
        if (!context.User.Identity.IsAuthenticated)
        {
            return Task.CompletedTask; // Not authenticated
        }

        // Check if the user has the required role (for example, "Admin")
        if (context.User.IsInRole("Admin")) // Replace "Admin" with your required role
        {
            context.Succeed(requirement); // Authorization succeeded
        }

        return Task.CompletedTask; // Authorization failed
    }
}