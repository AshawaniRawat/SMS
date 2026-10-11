import { httpClient } from '@/api/httpClient';
import type { ApiResponse } from '@/types/api';
import type { SchoolProfile, UpdateSchoolProfileRequest } from '../types/schoolProfile';

export async function getSchoolProfile() {
  const response = await httpClient.get<ApiResponse<SchoolProfile>>('/v1/school/profile');
  if (!response.data.data) throw new Error('School profile response did not contain data.');
  return response.data.data;
}

export async function updateSchoolProfile(request: UpdateSchoolProfileRequest) {
  const response = await httpClient.put<ApiResponse<SchoolProfile>>('/v1/school/profile', request);
  if (!response.data.data) throw new Error('School profile response did not contain data.');
  return response.data.data;
}
