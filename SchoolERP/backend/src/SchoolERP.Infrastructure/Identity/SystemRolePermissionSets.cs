using SchoolERP.Domain.Security;

namespace SchoolERP.Infrastructure.Identity;

public static class SystemRoleCodes
{
    public const string PlatformSuperAdmin = "PLATFORM_SUPER_ADMIN";
    public const string TenantOwner = "TENANT_OWNER";
    public const string SchoolAdmin = "SCHOOL_ADMIN";
    public const string Principal = "PRINCIPAL";
    public const string Teacher = "TEACHER";
    public const string Accountant = "ACCOUNTANT";
    public const string HrManager = "HR_MANAGER";
}

public static class SystemRolePermissionSets
{
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> All { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            [SystemRoleCodes.PlatformSuperAdmin] = PermissionCatalog.All.Select(x => x.Code).ToArray(),
            [SystemRoleCodes.TenantOwner] = PermissionCatalog.All.Where(x => x.Scope != Domain.Enums.PermissionScope.Platform).Select(x => x.Code).ToArray(),
            [SystemRoleCodes.SchoolAdmin] = PermissionCatalog.All.Where(x => x.Scope is Domain.Enums.PermissionScope.School or Domain.Enums.PermissionScope.Campus or Domain.Enums.PermissionScope.Self).Select(x => x.Code).ToArray(),
            [SystemRoleCodes.Principal] =
            [
                PermissionCatalog.School.View, PermissionCatalog.School.ViewDashboard, PermissionCatalog.Campus.View, PermissionCatalog.AcademicYear.View,PermissionCatalog.Class.View,PermissionCatalog.Class.Create,PermissionCatalog.Class.Edit,PermissionCatalog.Class.Activate,PermissionCatalog.Class.Deactivate,PermissionCatalog.Section.View,PermissionCatalog.Section.Create,PermissionCatalog.Section.Edit,PermissionCatalog.Section.Activate,PermissionCatalog.Section.Deactivate,PermissionCatalog.Subject.View,PermissionCatalog.Subject.Create,PermissionCatalog.Subject.Edit,PermissionCatalog.Subject.Activate,PermissionCatalog.Subject.Deactivate,PermissionCatalog.Subject.ManageClassMapping,PermissionCatalog.Department.View,PermissionCatalog.Department.Create,PermissionCatalog.Department.Edit,PermissionCatalog.Department.Activate,PermissionCatalog.Department.Deactivate,PermissionCatalog.House.View,PermissionCatalog.House.Create,PermissionCatalog.House.Edit,PermissionCatalog.House.Activate,PermissionCatalog.House.Deactivate,PermissionCatalog.Calendar.View,PermissionCatalog.Calendar.Create,PermissionCatalog.Calendar.Edit,PermissionCatalog.Calendar.Publish,PermissionCatalog.Calendar.Cancel,
                PermissionCatalog.Student.View, PermissionCatalog.Admission.View, PermissionCatalog.Attendance.View,
                PermissionCatalog.Exam.View, PermissionCatalog.Exam.VerifyMarks, PermissionCatalog.Exam.Publish,
                PermissionCatalog.Homework.View, PermissionCatalog.Employee.View, PermissionCatalog.Report.View,
                PermissionCatalog.Communication.View, PermissionCatalog.Communication.Broadcast
                ,PermissionCatalog.Audit.View
            ],
            [SystemRoleCodes.Teacher] =
            [
                PermissionCatalog.Class.View,PermissionCatalog.Section.View,PermissionCatalog.Subject.View,PermissionCatalog.Department.View,PermissionCatalog.House.View,PermissionCatalog.Calendar.View,PermissionCatalog.Student.View, PermissionCatalog.Attendance.View, PermissionCatalog.Attendance.Mark,
                PermissionCatalog.Timetable.View, PermissionCatalog.Exam.View, PermissionCatalog.Exam.EnterMarks,
                PermissionCatalog.Homework.View, PermissionCatalog.Homework.Create, PermissionCatalog.Homework.Edit,
                PermissionCatalog.LMS.View, PermissionCatalog.LMS.CreateContent, PermissionCatalog.Communication.View
            ],
            [SystemRoleCodes.Accountant] =
            [PermissionCatalog.Fee.View, PermissionCatalog.Fee.Collect, PermissionCatalog.Fee.EditCollection,
                PermissionCatalog.Report.View, PermissionCatalog.Report.Export],
            [SystemRoleCodes.HrManager] =
            [PermissionCatalog.Department.View,PermissionCatalog.Employee.View, PermissionCatalog.Employee.Create, PermissionCatalog.Employee.Edit,
                PermissionCatalog.HR.View, PermissionCatalog.HR.ManageLeave, PermissionCatalog.HR.ApproveLeave,
                PermissionCatalog.Payroll.View]
        };
}
