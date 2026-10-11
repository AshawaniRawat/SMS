using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using SchoolERP.Application.Features.Authentication;

namespace SchoolERP.Infrastructure.Identity;

public sealed class IdentityTokenEncoder : IIdentityTokenEncoder
{
    public string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public bool TryDecode(string encodedToken, out string token)
    {
        token = string.Empty;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            return token.Length > 0;
        }
        catch (FormatException) { return false; }
    }
}
