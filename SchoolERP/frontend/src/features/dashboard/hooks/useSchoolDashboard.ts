import { useQuery } from '@tanstack/react-query';
import { getSchoolDashboard } from '../api/getSchoolDashboard';
export function useSchoolDashboard(){return useQuery({queryKey:['school-dashboard'],queryFn:getSchoolDashboard,retry:(count,error:any)=>[403,409].includes(error?.response?.status)?false:count<1})}
