export type Department={id:string;name:string;code:string;shortName?:string|null;type:number;displayOrder:number;campusId?:string|null;campusName?:string|null;headName?:string|null;email?:string|null;phone?:string|null;location?:string|null;description?:string|null;status:number;activatedAt?:string|null;deactivatedAt?:string|null;archivedAt?:string|null;createdAt:string;updatedAt?:string|null};
export type DepartmentFilters={pageNumber?:number;pageSize?:number;campusId?:string;schoolWideOnly?:boolean;type?:number;status?:number;search?:string};
export type Paged<T>={items:T[];pageNumber:number;pageSize:number;totalCount:number;hasPreviousPage:boolean;hasNextPage:boolean};
export type ApiResponse<T>={data:T};
export type DepartmentInput={campusId?:string|null;name:string;code:string;shortName?:string|null;type:number;displayOrder:number;headName?:string|null;email?:string|null;phone?:string|null;location?:string|null;description?:string|null};
export const departmentTypes:Record<number,string>={1:"Academic",2:"Administrative",3:"Finance",4:"Human Resources",5:"Library",6:"Transport",7:"IT",8:"Security",9:"Operations",99:"Other"};
export const departmentStatuses:Record<number,string>={1:"Draft",2:"Active",3:"Inactive",4:"Archived"};
