import { httpClient } from '@/api/httpClient';
import type { ApiResponse } from '@/types/api';
import type { SchoolDashboard } from '../types/schoolDashboard';
export async function getSchoolDashboard(){const response=await httpClient.get<ApiResponse<SchoolDashboard>>('/v1/dashboard');if(!response.data.data)throw new Error('Dashboard response contained no data.');return response.data.data}
