import { Navigate, Outlet } from 'react-router-dom';
import { session } from '@/auth/session';
export function PermissionRoute({ permission }:{ permission:string }) {
  if (!session.token()) return <Navigate to="/login" replace/>;
  return session.hasPermission(permission) ? <Outlet/> : <Navigate to="/unauthorized" replace/>;
}
