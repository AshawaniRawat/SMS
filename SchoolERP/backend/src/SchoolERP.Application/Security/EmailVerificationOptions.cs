using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Application.Security;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";
    [Range(1, 168)] public int TokenLifespanHours { get; set; } = 24;
}
