namespace SchoolERP.Domain.Enums;
public enum DepartmentType { Academic=1,Administrative=2,Finance=3,HumanResources=4,Library=5,Transport=6,IT=7,Security=8,Operations=9,Other=99 }
public static class DepartmentTypeExtensions { public static bool IsAcademic(this DepartmentType value)=>value==DepartmentType.Academic; }
