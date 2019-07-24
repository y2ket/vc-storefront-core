using System;
using System.Linq;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Model.Security
{
    public static class UserExtensions
    {
        public static bool IsUserHasAnyRole(this User user, string role)
        {
            return user.IsUserHasAnyRoles(role);
        }
        public static bool IsUserHasAnyRoles(this User user, params string[] roles)
        {
            var result = user.IsAdministrator;
            if (!result && !user.Roles.IsNullOrEmpty())
            {
                result = user.Roles.Any(x => roles.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
            }
            return result;
        }
    }
}
