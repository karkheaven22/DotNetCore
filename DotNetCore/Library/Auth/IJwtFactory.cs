using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetCore
{
    public interface IJwtFactory
    {
        Task<AccessToken> GenerateEncodedToken(string userId, IList<string> roles);
    }
}