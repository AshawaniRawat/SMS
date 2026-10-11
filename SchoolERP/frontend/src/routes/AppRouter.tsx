import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { PlatformDashboardPage } from '@/features/platform-dashboard/pages/PlatformDashboardPage';
import { PermissionRoute } from './PermissionRoute';
import { AppLayout } from '@/components/layout/AppLayout';
import { SchoolDashboardPage } from '@/features/dashboard/pages/SchoolDashboardPage';
import { LoginPage } from '@/pages/LoginPage';
import { UnauthorizedPage } from '@/pages/UnauthorizedPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
export function AppRouter() { return <BrowserRouter><Routes><Route element={<AppLayout/>}><Route element={<PermissionRoute permission="School.ViewDashboard"/>}><Route path="/" element={<SchoolDashboardPage/>}/><Route path="/dashboard" element={<SchoolDashboardPage/>}/></Route><Route element={<PermissionRoute permission="Platform.ViewDashboard"/>}><Route path="/platform/dashboard" element={<PlatformDashboardPage/>}/></Route></Route><Route path="/login" element={<LoginPage/>}/><Route path="/unauthorized" element={<UnauthorizedPage/>}/><Route path="/not-found" element={<NotFoundPage/>}/><Route path="*" element={<NotFoundPage/>}/></Routes></BrowserRouter>; }
