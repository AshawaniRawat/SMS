using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Tenants;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;
using SchoolERP.Api.Security;
using SchoolERP.Domain.Security;

namespace SchoolERP.Api.Controllers.v1;

public sealed class TenantsController(ITenantService tenantService) : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(PermissionCatalog.Platform.ManageTenants)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Create(
        CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, ApiResponse<TenantDto>.Ok(tenant));
    }

    [HttpGet]
    [RequirePermission(PermissionCatalog.Tenant.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<TenantDto>>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await tenantService.ListAsync(new PagedRequest(pageNumber, pageSize), search, status, cancellationToken);
        return Ok(ApiResponse<PagedResult<TenantDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.Tenant.View)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetByIdAsync(id, cancellationToken);
        return tenant is null ? NotFound() : Ok(ApiResponse<TenantDto>.Ok(tenant));
    }

    [HttpGet("by-slug/{slug}")]
    [RequirePermission(PermissionCatalog.Tenant.View)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetBySlugAsync(slug, cancellationToken);
        return tenant is null ? NotFound() : Ok(ApiResponse<TenantDto>.Ok(tenant));
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionCatalog.Tenant.Activate)]
    public Task<ActionResult<ApiResponse<TenantDto>>> Activate(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => tenantService.ActivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/suspend")]
    [RequirePermission(PermissionCatalog.Tenant.Suspend)]
    public Task<ActionResult<ApiResponse<TenantDto>>> Suspend(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => tenantService.SuspendAsync(id, cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [RequirePermission(PermissionCatalog.Tenant.Activate)]
    public Task<ActionResult<ApiResponse<TenantDto>>> Reactivate(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => tenantService.ReactivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/archive")]
    [RequirePermission(PermissionCatalog.Tenant.Archive)]
    public Task<ActionResult<ApiResponse<TenantDto>>> Archive(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => tenantService.ArchiveAsync(id, cancellationToken));

    private async Task<ActionResult<ApiResponse<TenantDto>>> ChangeStatus(Func<Task<TenantDto?>> operation)
    {
        var tenant = await operation();
        return tenant is null ? NotFound() : Ok(ApiResponse<TenantDto>.Ok(tenant));
    }
}
