export type AuditOutcome="Succeeded"|"Failed"|"Denied";
export interface AuditLog{ id:string;tenantId?:string;schoolId?:string;campusId?:string;actorUserId?:string;actorDisplayName?:string;actorEmailMasked?:string;actorType:string;action:string;category:string;entityType?:string;entityId?:string;entityDisplayName?:string;outcome:AuditOutcome;description?:string;beforeJson?:string;afterJson?:string;metadataJson?:string;correlationId?:string;requestId?:string;traceId?:string;sessionId?:string;ipAddress?:string;userAgent?:string;occurredAtUtc:string }
export interface AuditFilters{fromUtc?:string;toUtc?:string;category?:string;action?:string;outcome?:AuditOutcome|"";entityType?:string;search?:string;pageNumber:number;pageSize?:number}
export interface Page<T>{items:T[];pageNumber:number;pageSize:number;totalCount:number;totalPages:number;hasPreviousPage:boolean;hasNextPage:boolean}
export interface Response<T>{success:boolean;data:T}
