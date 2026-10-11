const tokenKey = 'schoolerp.accessToken';
const permissionsKey = 'schoolerp.permissions';
export const session = {
  token:()=>localStorage.getItem(tokenKey),
  permissions:()=>{ try { return JSON.parse(localStorage.getItem(permissionsKey) ?? '[]') as string[]; } catch { return []; } },
  hasPermission:(permission:string)=>session.permissions().includes(permission),
  save:(token:string, permissions:string[])=>{localStorage.setItem(tokenKey,token);localStorage.setItem(permissionsKey,JSON.stringify(permissions));},
  clear:()=>{localStorage.removeItem(tokenKey);localStorage.removeItem(permissionsKey);}
};
