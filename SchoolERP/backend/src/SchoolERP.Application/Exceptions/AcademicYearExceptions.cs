namespace SchoolERP.Application.Exceptions;
public sealed class AcademicYearCodeConflictException : Exception { public AcademicYearCodeConflictException() : base("An academic year with this code already exists in the school.") { } }
public sealed class AcademicYearOverlapException : Exception { public AcademicYearOverlapException() : base("The academic year overlaps another session in the school.") { } }
public sealed class CurrentAcademicYearConflictException : Exception { public CurrentAcademicYearConflictException() : base("The school already has a current academic year.") { } }
