import { httpClient } from '@/api/httpClient';
import type { ApiResponse } from '@/types/api';
import type { PlatformDashboard } from '../types/platformDashboard';
export interface PlatformDashboardParams { trendMonths?:number; recentTenantLimit?:number }
export async function getPlatformDashboard(params:PlatformDashboardParams = {}) {
  const response = await httpClient.get<ApiResponse<PlatformDashboard>>('/v1/platform/dashboard', { params });
  if (!response.data.data) throw new Error('Dashboard response contained no data.');
  return response.data.data;
}
