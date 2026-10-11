import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getSchoolProfile, updateSchoolProfile } from '../api/schoolProfileApi';

export const schoolProfileKey = ['school-profile'] as const;
export function useSchoolProfile() {
  return useQuery({ queryKey: schoolProfileKey, queryFn: getSchoolProfile });
}
export function useUpdateSchoolProfile() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: updateSchoolProfile,
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: schoolProfileKey }),
        client.invalidateQueries({ queryKey: ['school-dashboard'] })
      ]);
    }
  });
}
