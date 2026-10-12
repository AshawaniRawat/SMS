using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Domain.Security;

public sealed record PermissionDefinition(Guid Id, string Code, string Name, string Module, string Action,
    string? Description, PermissionScope Scope, int SortOrder, string? Group = null, bool IsActive = true);

public static partial class PermissionCatalog
{
    public static class Platform { public const string ViewDashboard = "Platform.ViewDashboard", ManageTenants = "Platform.ManageTenants", ViewAudit = "Platform.ViewAudit", ManageSettings = "Platform.ManageSettings"; }
    public static class Tenant { public const string View = "Tenant.View", Edit = "Tenant.Edit", Activate = "Tenant.Activate", Suspend = "Tenant.Suspend", Archive = "Tenant.Archive"; }
    public static class School { public const string View = "School.View", ViewDashboard = "School.ViewDashboard", Create = "School.Create", Edit = "School.Edit", Activate = "School.Activate", Suspend = "School.Suspend", Archive = "School.Archive"; }
    public static class Campus { public const string View = "Campus.View", Create = "Campus.Create", Edit = "Campus.Edit", Activate = "Campus.Activate", Suspend = "Campus.Suspend", Archive = "Campus.Archive", SetMain = "Campus.SetMain"; }
    public static class AcademicYear { public const string View = "AcademicYear.View", Create = "AcademicYear.Create", Edit = "AcademicYear.Edit", Activate = "AcademicYear.Activate", SetCurrent = "AcademicYear.SetCurrent", Close = "AcademicYear.Close", Archive = "AcademicYear.Archive", ManageAdmissions = "AcademicYear.ManageAdmissions"; }
    public static class Class { public const string View="Class.View",Create="Class.Create",Edit="Class.Edit",Activate="Class.Activate",Deactivate="Class.Deactivate",Archive="Class.Archive"; }
    public static class Section { public const string View="Section.View",Create="Section.Create",Edit="Section.Edit",Activate="Section.Activate",Deactivate="Section.Deactivate",Archive="Section.Archive"; }
    public static class Subject { public const string View="Subject.View",Create="Subject.Create",Edit="Subject.Edit",Activate="Subject.Activate",Deactivate="Subject.Deactivate",Archive="Subject.Archive",ManageClassMapping="Subject.ManageClassMapping"; }
    public static class Department { public const string View="Department.View",Create="Department.Create",Edit="Department.Edit",Activate="Department.Activate",Deactivate="Department.Deactivate",Archive="Department.Archive"; }
    public static class House { public const string View="House.View",Create="House.Create",Edit="House.Edit",Activate="House.Activate",Deactivate="House.Deactivate",Archive="House.Archive"; }
    public static class Calendar { public const string View="Calendar.View",Create="Calendar.Create",Edit="Calendar.Edit",Publish="Calendar.Publish",Cancel="Calendar.Cancel",Archive="Calendar.Archive"; }
    public static class User { public const string View = "User.View", Create = "User.Create", Edit = "User.Edit", Activate = "User.Activate", Suspend = "User.Suspend", Deactivate = "User.Deactivate", AssignRole = "User.AssignRole"; }
    public static class Role { public const string View = "Role.View", Create = "Role.Create", Edit = "Role.Edit", Activate = "Role.Activate", Deactivate = "Role.Deactivate", Archive = "Role.Archive", AssignUser = "Role.AssignUser", ManagePermissions = "Role.ManagePermissions"; }
    public static class Permission { public const string View = "Permission.View"; }
    public static class Student { public const string View = "Student.View", Create = "Student.Create", Edit = "Student.Edit", Archive = "Student.Archive", Promote = "Student.Promote", Export = "Student.Export"; }
    public static class Admission { public const string View = "Admission.View", Create = "Admission.Create", Edit = "Admission.Edit", Approve = "Admission.Approve", Reject = "Admission.Reject", ConvertToStudent = "Admission.ConvertToStudent"; }
    public static class Attendance { public const string View = "Attendance.View", Mark = "Attendance.Mark", Edit = "Attendance.Edit", Export = "Attendance.Export", Approve = "Attendance.Approve"; }
    public static class Fee { public const string View = "Fee.View", Configure = "Fee.Configure", Collect = "Fee.Collect", EditCollection = "Fee.EditCollection", Refund = "Fee.Refund", Waive = "Fee.Waive", Export = "Fee.Export"; }
    public static class Timetable { public const string View = "Timetable.View", Create = "Timetable.Create", Edit = "Timetable.Edit", Publish = "Timetable.Publish"; }
    public static class Exam { public const string View = "Exam.View", Create = "Exam.Create", Edit = "Exam.Edit", EnterMarks = "Exam.EnterMarks", VerifyMarks = "Exam.VerifyMarks", Publish = "Exam.Publish", Export = "Exam.Export"; }
    public static class Homework { public const string View = "Homework.View", Create = "Homework.Create", Edit = "Homework.Edit", Publish = "Homework.Publish"; }
    public static class LMS { public const string View = "LMS.View", CreateContent = "LMS.CreateContent", EditContent = "LMS.EditContent", PublishContent = "LMS.PublishContent"; }
    public static class Employee { public const string View = "Employee.View", Create = "Employee.Create", Edit = "Employee.Edit", Archive = "Employee.Archive"; }
    public static class HR { public const string View = "HR.View", ManageLeave = "HR.ManageLeave", ApproveLeave = "HR.ApproveLeave"; }
    public static class Payroll { public const string View = "Payroll.View", Configure = "Payroll.Configure", Process = "Payroll.Process", Approve = "Payroll.Approve", Export = "Payroll.Export"; }
    public static class Transport { public const string View = "Transport.View", Configure = "Transport.Configure", AssignVehicle = "Transport.AssignVehicle", AssignStudent = "Transport.AssignStudent", TrackVehicle = "Transport.TrackVehicle"; }
    public static class Library { public const string View = "Library.View", ManageCatalog = "Library.ManageCatalog", IssueBook = "Library.IssueBook", ReturnBook = "Library.ReturnBook", ManageFine = "Library.ManageFine"; }
    public static class Inventory { public const string View = "Inventory.View", Create = "Inventory.Create", Edit = "Inventory.Edit", Issue = "Inventory.Issue", Receive = "Inventory.Receive", Archive = "Inventory.Archive"; }
    public static class Hostel { public const string View = "Hostel.View", Configure = "Hostel.Configure", AssignStudent = "Hostel.AssignStudent", ManageRoom = "Hostel.ManageRoom"; }
    public static class Communication { public const string View = "Communication.View", Send = "Communication.Send", Broadcast = "Communication.Broadcast", ManageTemplate = "Communication.ManageTemplate"; }
    public static class Report { public const string View = "Report.View", Export = "Report.Export"; }
    public static class Audit { public const string View = "Audit.View", PlatformView = "Audit.PlatformView", Export = "Audit.Export"; }

    public static IReadOnlyList<PermissionDefinition> All { get; } = Build();

    public static PermissionDefinition? FindByCode(string code) =>
        All.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.Ordinal));

    public static IReadOnlyCollection<string> Validate()
    {
        var errors = new List<string>();
        foreach (var duplicate in All.GroupBy(x => x.Code, StringComparer.Ordinal).Where(x => x.Count() > 1))
            errors.Add($"Duplicate permission code: {duplicate.Key}");
        foreach (var item in All)
        {
            if (!CodePattern().IsMatch(item.Code)) errors.Add($"Invalid permission code: {item.Code}");
            if (item.Code != $"{item.Module}.{item.Action}") errors.Add($"Code does not match module/action: {item.Code}");
            if (string.IsNullOrWhiteSpace(item.Name)) errors.Add($"Permission name is empty: {item.Code}");
            if (string.IsNullOrWhiteSpace(item.Module)) errors.Add($"Permission module is empty: {item.Code}");
            if (string.IsNullOrWhiteSpace(item.Action)) errors.Add($"Permission action is empty: {item.Code}");
            if (!Enum.IsDefined(item.Scope)) errors.Add($"Permission scope is invalid: {item.Code}");
            if (item.SortOrder < 0) errors.Add($"Permission sort order is negative: {item.Code}");
        }
        return new ReadOnlyCollection<string>(errors);
    }

    private static IReadOnlyList<PermissionDefinition> Build()
    {
        var rows = new List<PermissionDefinition>();
        Add(rows, PermissionScope.Platform, Platform.ViewDashboard, Platform.ManageTenants, Platform.ViewAudit, Platform.ManageSettings);
        Add(rows, PermissionScope.Platform, Tenant.View, Tenant.Edit, Tenant.Activate, Tenant.Suspend, Tenant.Archive);
        Add(rows, PermissionScope.School, School.View, School.Create, School.Edit, School.Activate, School.Suspend, School.Archive);
        Add(rows, PermissionScope.Campus, Campus.View, Campus.Create, Campus.Edit, Campus.Activate, Campus.Suspend, Campus.Archive, Campus.SetMain);
        Add(rows, PermissionScope.School, AcademicYear.View, AcademicYear.Create, AcademicYear.Edit, AcademicYear.Activate, AcademicYear.SetCurrent, AcademicYear.Close, AcademicYear.Archive, AcademicYear.ManageAdmissions);
        Add(rows, PermissionScope.School, User.View, User.Create, User.Edit, User.Activate, User.Suspend, User.Deactivate, User.AssignRole);
        Add(rows, PermissionScope.School, Role.View, Role.Create, Role.Edit, Role.Activate, Role.Deactivate, Role.Archive, Role.AssignUser);
        Add(rows, PermissionScope.School, Student.View, Student.Create, Student.Edit, Student.Archive, Student.Promote, Student.Export);
        Add(rows, PermissionScope.School, Admission.View, Admission.Create, Admission.Edit, Admission.Approve, Admission.Reject, Admission.ConvertToStudent);
        Add(rows, PermissionScope.Campus, Attendance.View, Attendance.Mark, Attendance.Edit, Attendance.Export, Attendance.Approve);
        Add(rows, PermissionScope.School, Fee.View, Fee.Configure, Fee.Collect, Fee.EditCollection, Fee.Refund, Fee.Waive, Fee.Export);
        Add(rows, PermissionScope.Campus, Timetable.View, Timetable.Create, Timetable.Edit, Timetable.Publish);
        Add(rows, PermissionScope.School, Exam.View, Exam.Create, Exam.Edit, Exam.EnterMarks, Exam.VerifyMarks, Exam.Publish, Exam.Export);
        Add(rows, PermissionScope.Campus, Homework.View, Homework.Create, Homework.Edit, Homework.Publish);
        Add(rows, PermissionScope.School, LMS.View, LMS.CreateContent, LMS.EditContent, LMS.PublishContent);
        Add(rows, PermissionScope.School, Employee.View, Employee.Create, Employee.Edit, Employee.Archive);
        Add(rows, PermissionScope.Tenant, HR.View, HR.ManageLeave, HR.ApproveLeave);
        Add(rows, PermissionScope.Tenant, Payroll.View, Payroll.Configure, Payroll.Process, Payroll.Approve, Payroll.Export);
        Add(rows, PermissionScope.School, Transport.View, Transport.Configure, Transport.AssignVehicle, Transport.AssignStudent, Transport.TrackVehicle);
        Add(rows, PermissionScope.School, Library.View, Library.ManageCatalog, Library.IssueBook, Library.ReturnBook, Library.ManageFine);
        Add(rows, PermissionScope.School, Inventory.View, Inventory.Create, Inventory.Edit, Inventory.Issue, Inventory.Receive, Inventory.Archive);
        Add(rows, PermissionScope.Campus, Hostel.View, Hostel.Configure, Hostel.AssignStudent, Hostel.ManageRoom);
        Add(rows, PermissionScope.School, Communication.View, Communication.Send, Communication.Broadcast, Communication.ManageTemplate);
        Add(rows, PermissionScope.School, Report.View, Report.Export);
        Add(rows, PermissionScope.School, Audit.View, Audit.Export);
        // Appended to preserve deterministic IDs of the P1-18 catalog.
        Add(rows, PermissionScope.School, Role.ManagePermissions, Permission.View);
        Add(rows, PermissionScope.School, School.ViewDashboard);
        Add(rows,PermissionScope.School,Class.View,Class.Create,Class.Edit,Class.Activate,Class.Deactivate,Class.Archive);
        Add(rows,PermissionScope.School,Section.View,Section.Create,Section.Edit,Section.Activate,Section.Deactivate,Section.Archive);
        Add(rows,PermissionScope.School,Subject.View,Subject.Create,Subject.Edit,Subject.Activate,Subject.Deactivate,Subject.Archive,Subject.ManageClassMapping);
        Add(rows,PermissionScope.School,Department.View,Department.Create,Department.Edit,Department.Activate,Department.Deactivate,Department.Archive);
        Add(rows,PermissionScope.School,House.View,House.Create,House.Edit,House.Activate,House.Deactivate,House.Archive);
        Add(rows,PermissionScope.School,Calendar.View,Calendar.Create,Calendar.Edit,Calendar.Publish,Calendar.Cancel,Calendar.Archive);
        Add(rows,PermissionScope.Platform,Audit.PlatformView);
        return rows.AsReadOnly();
    }

    private static void Add(List<PermissionDefinition> rows, PermissionScope scope, params string[] codes)
    {
        foreach (var code in codes)
        {
            var parts = code.Split('.');
            var number = rows.Count + 1;
            var actionName = PascalWords().Replace(parts[1], " $1").Trim();
            var moduleName = PascalWords().Replace(parts[0], " $1").Trim();
            rows.Add(new PermissionDefinition(
                Guid.Parse($"b1180000-0000-0000-0000-{number:D12}"), code,
                $"{actionName} {moduleName}", parts[0], parts[1],
                $"Allows the {actionName.ToLowerInvariant()} operation for {moduleName.ToLowerInvariant()} resources.",
                scope, number * 10));
        }
    }

    [GeneratedRegex("(?<=[a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalWords();
    [GeneratedRegex("^[A-Z][A-Za-z0-9]*\\.[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
