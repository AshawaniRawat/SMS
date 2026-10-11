using SchoolERP.Domain.Enums;

namespace SchoolERP.Infrastructure.Identity;

public static class CoreRoleCatalog
{
    public sealed record Definition(Guid Id, string Code, string DisplayName, RoleType Type, int SortOrder);

    public static IReadOnlyList<Definition> All { get; } =
    [
        Role(1, SystemRoleCodes.PlatformSuperAdmin, "Platform Super Admin", RoleType.Platform),
        Role(2, SystemRoleCodes.TenantOwner, "Tenant Owner", RoleType.Tenant), Role(3, SystemRoleCodes.SchoolAdmin, "School Admin", RoleType.School),
        Role(4, SystemRoleCodes.Principal, "Principal", RoleType.School), Role(5, "VICE_PRINCIPAL", "Vice Principal", RoleType.School),
        Role(6, "ACADEMIC_COORDINATOR", "Academic Coordinator", RoleType.School), Role(7, "ADMISSION_MANAGER", "Admission Manager", RoleType.School),
        Role(8, "RECEPTIONIST", "Receptionist", RoleType.School), Role(9, SystemRoleCodes.Accountant, "Accountant", RoleType.School),
        Role(10, "FINANCE_MANAGER", "Finance Manager", RoleType.Tenant), Role(11, SystemRoleCodes.HrManager, "HR Manager", RoleType.Tenant),
        Role(12, SystemRoleCodes.Teacher, "Teacher", RoleType.School), Role(13, "CLASS_TEACHER", "Class Teacher", RoleType.School),
        Role(14, "SUBJECT_TEACHER", "Subject Teacher", RoleType.School), Role(15, "EXAM_CONTROLLER", "Exam Controller", RoleType.School),
        Role(16, "LIBRARIAN", "Librarian", RoleType.School), Role(17, "TRANSPORT_MANAGER", "Transport Manager", RoleType.Tenant),
        Role(18, "DRIVER", "Driver", RoleType.Campus), Role(19, "HOSTEL_WARDEN", "Hostel Warden", RoleType.Campus),
        Role(20, "INVENTORY_MANAGER", "Inventory Manager", RoleType.Tenant), Role(21, "IT_ADMIN", "IT Admin", RoleType.Tenant),
        Role(22, "COUNSELLOR", "Counsellor", RoleType.School), Role(23, "NURSE", "Nurse", RoleType.Campus),
        Role(24, "SECURITY_GUARD", "Security Guard", RoleType.Campus), Role(25, "PARENT", "Parent", RoleType.School),
        Role(26, "STUDENT", "Student", RoleType.School), Role(27, "AUDITOR", "Auditor", RoleType.Tenant)
    ];

    private static Definition Role(int value, string code, string displayName, RoleType type) =>
        new(Guid.Parse($"a1170000-0000-0000-0000-{value:D12}"), code, displayName, type, value * 10);
}
