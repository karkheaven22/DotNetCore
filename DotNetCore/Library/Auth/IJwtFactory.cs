using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotNetCore
{
    public interface IJwtFactory
    {
        Task<AccessToken> GenerateEncodedToken(string userId, IList<string> roles);
        Task<string> GenerateEncodedToken(string userName, ClaimsIdentity identity);
        ClaimsIdentity GenerateClaimsIdentity(string userName, string id);
    }
}