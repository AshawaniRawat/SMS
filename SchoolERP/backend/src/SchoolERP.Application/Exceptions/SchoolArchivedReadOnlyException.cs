namespace SchoolERP.Application.Exceptions;

public sealed class SchoolArchivedReadOnlyException()
    : Exception("School.ArchivedReadOnly: Archived schools cannot be modified.");
