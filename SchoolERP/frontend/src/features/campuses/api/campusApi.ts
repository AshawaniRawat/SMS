import {httpClient} from '@/api/httpClient';import type{ApiResponse}from '@/types/api';import type{CampusDetails,CampusFilters,CampusListItem,CampusRequest,PagedResult}from '../types/campus';
const data=<T>(r:{data:ApiResponse<T>})=>{if(!r.data.data)throw new Error('Campus response did not contain data.');return r.data.data};
export const getCampuses=async(filters:CampusFilters)=>data(await httpClient.get<ApiResponse<PagedResult<CampusListItem>>>('/v1/campuses',{params:filters}));
export const getCampus=async(id:string)=>data(await httpClient.get<ApiResponse<CampusDetails>>(`/v1/campuses/${id}`));
export const createCampus=async(body:CampusRequest)=>data(await httpClient.post<ApiResponse<CampusDetails>>('/v1/campuses',body));
export const updateCampus=async({id,body}:{id:string;body:CampusRequest})=>data(await httpClient.put<ApiResponse<CampusDetails>>(`/v1/campuses/${id}`,body));
export const campusAction=async({id,action}:{id:string;action:string})=>data(await httpClient.post<ApiResponse<CampusDetails>>(`/v1/campuses/${id}/${action}`));
