# Initial system-role permission defaults

These platform-managed defaults are additive and may evolve. Seeding inserts missing expected grants and never edits custom tenant roles or automatically removes unexpected historical system grants.

## PLATFORM_SUPER_ADMIN

Every active system permission in `PermissionCatalog`. New active catalog permissions are added on the next seed. Access is still granted only when this role is explicitly assigned.

## TENANT_OWNER

Every active non-Platform permission. This provides broad tenant, school, campus, and self-scope administration without platform operations.

## SCHOOL_ADMIN

Every active School, Campus, and Self permission. Platform and Tenant-scope permissions are excluded by the compatibility model.

## PRINCIPAL

`School.View`, `Campus.View`, `AcademicYear.View`, `Student.View`, `Admission.View`, `Attendance.View`, `Exam.View`, `Exam.VerifyMarks`, `Exam.Publish`, `Homework.View`, `Employee.View`, `Report.View`, `Communication.View`, `Communication.Broadcast`.

## TEACHER

`Student.View`, `Attendance.View`, `Attendance.Mark`, `Timetable.View`, `Exam.View`, `Exam.EnterMarks`, `Homework.View`, `Homework.Create`, `Homework.Edit`, `LMS.View`, `LMS.CreateContent`, `Communication.View`.

## ACCOUNTANT

`Fee.View`, `Fee.Collect`, `Fee.EditCollection`, `Report.View`, `Report.Export`. `Fee.Refund` and `Fee.Waive` are intentionally excluded.

## HR_MANAGER

`Employee.View`, `Employee.Create`, `Employee.Edit`, `HR.View`, `HR.ManageLeave`, `HR.ApproveLeave`, `Payroll.View`. Payroll processing and approval remain separate grants.

Parent and Student receive no broad defaults until appropriate self-scope permissions are designed.
