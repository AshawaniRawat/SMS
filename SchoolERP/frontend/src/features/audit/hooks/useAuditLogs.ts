import{useQuery}from"@tanstack/react-query";import{auditApi}from"../api/auditApi";import type{AuditFilters}from"../types";
export const useAuditLogs=(filters:AuditFilters,platform=false)=>useQuery({queryKey:["audit-logs",platform,filters],queryFn:()=>auditApi.list(filters,platform),retry:false});
export const useAuditLog=(id:string|undefined,platform=false)=>useQuery({queryKey:["audit-log",platform,id],queryFn:()=>auditApi.get(id!,platform),enabled:!!id,retry:false});
