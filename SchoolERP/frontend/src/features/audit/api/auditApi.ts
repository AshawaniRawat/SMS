import{httpClient}from"@/api/httpClient";import type{AuditFilters,AuditLog,Page,Response}from"../types";
const unwrap=<T>(r:{data:Response<T>})=>r.data.data;
export const auditApi={list:async(filters:AuditFilters,platform=false)=>unwrap(await httpClient.get<Response<Page<AuditLog>>>(`${platform?"/v1/platform":"/v1"}/audit-logs`,{params:filters})),get:async(id:string,platform=false)=>unwrap(await httpClient.get<Response<AuditLog>>(`${platform?"/v1/platform":"/v1"}/audit-logs/${id}`))};
