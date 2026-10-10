namespace SchoolERP.Application.Exceptions;
public sealed class CampusCodeConflictException : Exception { public CampusCodeConflictException() : base("A campus with this code already exists in the school.") { } }
public sealed class MainCampusConflictException : Exception { public MainCampusConflictException() : base("The school already has a main campus.") { } }
public sealed class ParentNotOperationalException(string message) : Exception(message);
