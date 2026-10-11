export type TenantStatus = 'Pending' | 'Active' | 'Suspended' | 'Archived' | number;
export interface PlatformSummary { totalTenants:number; activeTenants:number; pendingTenants:number; suspendedTenants:number; archivedTenants:number; totalSchools:number; activeSchools:number; totalCampuses:number; activeCampuses:number; totalUsers:number; activeUsers:number; totalAcademicYears:number }
export interface TenantStatusSummary { pending:number; active:number; suspended:number; archived:number }
export interface PlatformUserSummary { totalUsers:number; activeUsers:number; pendingUsers:number; suspendedUsers:number; lockedUsers:number; deactivatedUsers:number; platformUsers:number; tenantUsers:number; unconfirmedEmailUsers:number; activeRefreshSessions:number }
export interface TenantTrendPoint { period:string; count:number }
export interface RecentTenant { tenantId:string; name:string; slug:string; status:TenantStatus; createdAt:string; activatedAt?:string; schoolCount:number; campusCount:number }
export interface TenantAttention { tenantId:string; name:string; status:TenantStatus; reason:string; since?:string }
export interface SystemHealthSummary { apiHealthy:boolean; databaseHealthy:boolean; overallStatus:string; checkedAtUtc:string }
export interface PlatformDashboard { summary:PlatformSummary; tenantStatus:TenantStatusSummary; users:PlatformUserSummary; onboardingTrend:TenantTrendPoint[]; recentTenants:RecentTenant[]; tenantsRequiringAttention:TenantAttention[]; systemHealth:SystemHealthSummary }
