using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using SchoolERP.Application.Security;
namespace SchoolERP.Infrastructure.Identity;
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public string GenerateToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
