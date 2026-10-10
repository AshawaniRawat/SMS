using SchoolERP.Domain.Enums;

namespace SchoolERP.Domain.Exceptions;

public sealed class InvalidUserAccountStatusTransitionException(UserAccountStatus current, UserAccountStatus target)
    : DomainException($"A user account cannot transition from {current} to {target}.");
