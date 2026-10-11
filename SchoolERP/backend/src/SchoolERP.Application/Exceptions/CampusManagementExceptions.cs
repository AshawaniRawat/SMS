namespace SchoolERP.Application.Exceptions;

public sealed class CampusArchivedReadOnlyException()
    : Exception("Campus.ArchivedReadOnly: Archived campuses cannot be modified.");
public sealed class MainCampusCannotBeSuspendedException()
    : Exception("Campus.MainCampusCannotBeSuspended: Assign another main campus before suspension.");
public sealed class MainCampusCannotBeArchivedException()
    : Exception("Campus.MainCampusCannotBeArchived: Assign another main campus before archiving.");
