using System.Security.Claims;
using System.Security.Principal;

namespace Collaborative_Task_Management_System.Extensions
{
    public static class IdentityExtensions
    {
        public static string GetUserId(this IIdentity identity)
        {
            if (identity == null)
            {
                return null;
            }

            var claimsIdentity = identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return null;
            }

            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value;
        }
    }
}