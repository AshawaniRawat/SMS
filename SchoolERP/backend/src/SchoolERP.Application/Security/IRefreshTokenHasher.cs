namespace SchoolERP.Application.Security;
public interface IRefreshTokenHasher
{
    string GenerateToken();
    string Hash(string token);
}
