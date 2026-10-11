import { useQuery } from '@tanstack/react-query';
import { getPlatformDashboard, type PlatformDashboardParams } from '../api/getPlatformDashboard';
export function usePlatformDashboard(params:PlatformDashboardParams = {}) {
  return useQuery({ queryKey:['platform-dashboard', params], queryFn:()=>getPlatformDashboard(params), retry:(count,error:any)=>error?.response?.status === 403 ? false : count < 1 });
}
