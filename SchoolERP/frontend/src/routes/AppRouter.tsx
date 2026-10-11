import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { PlatformDashboardPage } from '@/features/platform-dashboard/pages/PlatformDashboardPage';
import { PermissionRoute } from './PermissionRoute';
import { AppLayout } from '@/components/layout/AppLayout';
import { SchoolDashboardPage } from '@/features/dashboard/pages/SchoolDashboardPage';
import { LoginPage } from '@/pages/LoginPage';
import { SchoolProfilePage } from '@/features/school-profile/pages/SchoolProfilePage';
import { CampusListPage } from '@/features/campuses/pages/CampusListPage';
import { CampusCreatePage } from '@/features/campuses/pages/CampusCreatePage';
import { CampusDetailsPage } from '@/features/campuses/pages/CampusDetailsPage';
import { CampusEditPage } from '@/features/campuses/pages/CampusEditPage';
import {AcademicYearListPage,AcademicYearCreatePage,AcademicYearDetailsPage,AcademicYearEditPage} from '@/features/academic-years/academicYears';
import { UnauthorizedPage } from '@/pages/UnauthorizedPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
export function AppRouter() { return <BrowserRouter><Routes><Route element={<AppLayout/>}><Route element={<PermissionRoute permission="School.ViewDashboard"/>}><Route path="/" element={<SchoolDashboardPage/>}/><Route path="/dashboard" element={<SchoolDashboardPage/>}/></Route><Route element={<PermissionRoute permission="School.View"/>}><Route path="/settings/school" element={<SchoolProfilePage/>}/></Route><Route element={<PermissionRoute permission="Campus.View"/>}><Route path="/campuses" element={<CampusListPage/>}/><Route path="/campuses/:id" element={<CampusDetailsPage/>}/><Route path="/campuses/:id/edit" element={<CampusEditPage/>}/></Route><Route element={<PermissionRoute permission="Campus.Create"/>}><Route path="/campuses/new" element={<CampusCreatePage/>}/></Route><Route element={<PermissionRoute permission="AcademicYear.View"/>}><Route path="/academic-years" element={<AcademicYearListPage/>}/><Route path="/academic-years/:id" element={<AcademicYearDetailsPage/>}/><Route path="/academic-years/:id/edit" element={<AcademicYearEditPage/>}/></Route><Route element={<PermissionRoute permission="AcademicYear.Create"/>}><Route path="/academic-years/new" element={<AcademicYearCreatePage/>}/></Route><Route element={<PermissionRoute permission="Platform.ViewDashboard"/>}><Route path="/platform/dashboard" element={<PlatformDashboardPage/>}/></Route></Route><Route path="/login" element={<LoginPage/>}/><Route path="/unauthorized" element={<UnauthorizedPage/>}/><Route path="/not-found" element={<NotFoundPage/>}/><Route path="*" element={<NotFoundPage/>}/></Routes></BrowserRouter>; }
