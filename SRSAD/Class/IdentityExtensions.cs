
using System.Security.Claims;
namespace System.Security.Principal
{
    public static class IdentityExtensions
    {
        
        public static string GetFirstName(this IIdentity principal)
        {
            var claim = ((ClaimsIdentity)principal);
            var firstName = claim.FindFirst(c => c.Type == "FirstName");
            return firstName?.Value;
        }

        public static string GetLastName(this IIdentity principal)
        {
            var claim = ((ClaimsIdentity)principal);
            var lastName = claim.FindFirst(c => c.Type == "LastName");
            return lastName?.Value;
        }
        
    }
}