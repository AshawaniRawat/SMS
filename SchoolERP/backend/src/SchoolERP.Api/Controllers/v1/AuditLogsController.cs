using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Api.Security;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Audit;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;
using SchoolERP.Shared.Models;
namespace SchoolERP.Api.Controllers.v1;

[ApiController,Authorize,Route("api/v1/audit-logs"),RequirePermission(PermissionCatalog.Audit.View)]
public sealed class AuditLogsController(IAuditQueryService service):ControllerBase
{
    /// <summary>Returns audit history within the caller's tenant, school, and campus scope.</summary>
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> List([FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,[FromQuery]string? category=null,[FromQuery]string? action=null,[FromQuery]AuditOutcome? outcome=null,[FromQuery]string? entityType=null,[FromQuery]string? search=null,[FromQuery]int pageNumber=1,[FromQuery]int pageSize=50,CancellationToken ct=default)=>Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(await service.ListAsync(new(fromUtc,toUtc,category,action,outcome,entityType,search,pageNumber,pageSize),false,ct)));
    /// <summary>Returns one scoped audit record. Cross-scope identifiers return 404.</summary>
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<AuditLogDto>>> Get(Guid id,CancellationToken ct){var item=await service.GetAsync(id,false,ct);return item is null?NotFound():Ok(ApiResponse<AuditLogDto>.Ok(item));}
}

[ApiController,Authorize,Route("api/v1/platform/audit-logs"),RequirePermission(PermissionCatalog.Audit.PlatformView)]
public sealed class PlatformAuditLogsController(IAuditQueryService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> List([FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,[FromQuery]string? category=null,[FromQuery]string? action=null,[FromQuery]AuditOutcome? outcome=null,[FromQuery]string? entityType=null,[FromQuery]string? search=null,[FromQuery]int pageNumber=1,[FromQuery]int pageSize=50,CancellationToken ct=default)=>Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(await service.ListAsync(new(fromUtc,toUtc,category,action,outcome,entityType,search,pageNumber,pageSize),true,ct)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<AuditLogDto>>> Get(Guid id,CancellationToken ct){var item=await service.GetAsync(id,true,ct);return item is null?NotFound():Ok(ApiResponse<AuditLogDto>.Ok(item));}
}
