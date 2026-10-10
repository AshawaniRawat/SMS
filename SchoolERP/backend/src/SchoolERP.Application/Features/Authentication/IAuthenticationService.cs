using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Authentication;

public interface IAuthenticationService
{
    Task<Result<LoginResponse>> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
