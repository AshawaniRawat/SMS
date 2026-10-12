namespace SchoolERP.Domain.Security;
public static class AuditActions
{
    public static class Authentication { public const string LoginSucceeded="Authentication.LoginSucceeded", Logout="Authentication.Logout"; }
    public static class Tenant { public const string Created="Tenant.Created", Updated="Tenant.Updated", StatusChanged="Tenant.StatusChanged"; }
    public static class School { public const string Created="School.Created", ProfileUpdated="School.ProfileUpdated", StatusChanged="School.StatusChanged"; }
    public static class Campus { public const string Created="Campus.Created", Updated="Campus.Updated", SetMain="Campus.SetMain", StatusChanged="Campus.StatusChanged"; }
    public static class AcademicYear { public const string Created="AcademicYear.Created", Updated="AcademicYear.Updated", SetCurrent="AcademicYear.SetCurrent", StatusChanged="AcademicYear.StatusChanged"; }
    public static class Identity { public const string UserCreated="User.Created", UserUpdated="User.Updated", RoleCreated="Role.Created", RoleUpdated="Role.Updated"; }
    public static class Role { public const string PermissionAssigned="Role.PermissionAssigned", PermissionRemoved="Role.PermissionRemoved"; }
    public static class Academic { public const string ClassChanged="Class.Changed", SectionChanged="Section.Changed", SubjectChanged="Subject.Changed", SubjectAssignedToClass="Subject.AssignedToClass", DepartmentChanged="Department.Changed", HouseChanged="House.Changed", CalendarEventChanged="Calendar.EventChanged"; }
}
