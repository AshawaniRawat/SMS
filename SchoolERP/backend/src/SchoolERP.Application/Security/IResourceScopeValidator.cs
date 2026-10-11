using SchoolERP.Application.Abstractions;

namespace SchoolERP.Application.Security;

public interface IResourceScopeValidator
{
    Guid? SchoolId { get; }
    Guid? CampusId { get; }
    bool CanAccessSchool(Guid schoolId);
    bool CanAccessCampus(Guid schoolId, Guid campusId);
}

public sealed class ResourceScopeValidator(ICurrentUser currentUser) : IResourceScopeValidator
{
    public Guid? SchoolId => currentUser.SchoolId;
    public Guid? CampusId => currentUser.CampusId;

    public bool CanAccessSchool(Guid schoolId) => !SchoolId.HasValue || SchoolId.Value == schoolId;

    public bool CanAccessCampus(Guid schoolId, Guid campusId) =>
        CanAccessSchool(schoolId) && (!CampusId.HasValue || CampusId.Value == campusId);
}
