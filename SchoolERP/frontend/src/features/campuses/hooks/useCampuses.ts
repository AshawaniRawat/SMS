import{useMutation,useQuery,useQueryClient}from'@tanstack/react-query';import{campusAction,createCampus,getCampus,getCampuses,updateCampus}from'../api/campusApi';import type{CampusFilters}from'../types/campus';
export const useCampuses=(filters:CampusFilters)=>useQuery({queryKey:['campuses',filters],queryFn:()=>getCampuses(filters)});
export const useCampus=(id:string)=>useQuery({queryKey:['campus',id],queryFn:()=>getCampus(id),enabled:!!id});
function invalidator(){const q=useQueryClient();return async(_:unknown,v:{id?:string})=>{await Promise.all([q.invalidateQueries({queryKey:['campuses']}),q.invalidateQueries({queryKey:['school-dashboard']}),...(v.id?[q.invalidateQueries({queryKey:['campus',v.id]})]:[])])}}
export function useCreateCampus(){const q=useQueryClient();return useMutation({mutationFn:createCampus,onSuccess:async()=>{await Promise.all([q.invalidateQueries({queryKey:['campuses']}),q.invalidateQueries({queryKey:['school-dashboard']})])}})}
export function useUpdateCampus(){return useMutation({mutationFn:updateCampus,onSuccess:invalidator()})}
export function useCampusAction(){return useMutation({mutationFn:campusAction,onSuccess:invalidator()})}
