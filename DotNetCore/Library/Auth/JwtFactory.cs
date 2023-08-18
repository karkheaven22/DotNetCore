using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotNetCore
{
    public class JwtFactory : IJwtFactory
    {
        private readonly JwtIssuerOptions _jwtOptions;

        public JwtFactory(IOptions<JwtIssuerOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
            ThrowIfInvalidOptions(_jwtOptions);
        }

        public async Task<AccessToken> GenerateEncodedToken(string userId, IList<string> roles)
        {
            List<Claim> claims = GenerateClaimsIdentity(userId, roles);
            //claims.Add(new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()));

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(
                //issuer: _jwtOptions.Issuer,
                //audience: _jwtOptions.Audience,
                claims: claims,
                expires: _jwtOptions.Expiration,
                signingCredentials: _jwtOptions.SigningCredentials);

            var encodedJwt = await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(jwt));
            return new AccessToken(encodedJwt, (int)_jwtOptions.ValidFor.TotalSeconds);
        }

        private static List<Claim> GenerateClaimsIdentity(string userId, IList<string> roles)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, userId)
            };
            ((List<string>)roles).ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));
            return claims;
        }

        /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
        private static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() -
                            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                            .TotalSeconds);

        private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.ValidFor <= TimeSpan.Zero)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
            }
        }
    }
}