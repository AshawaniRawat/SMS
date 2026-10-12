import { BrowserRouter, Routes, Route } from "react-router-dom";
import { PlatformDashboardPage } from "@/features/platform-dashboard/pages/PlatformDashboardPage";
import { PermissionRoute } from "./PermissionRoute";
import { AppLayout } from "@/components/layout/AppLayout";
import { SchoolDashboardPage } from "@/features/dashboard/pages/SchoolDashboardPage";
import { LoginPage } from "@/pages/LoginPage";
import { SchoolProfilePage } from "@/features/school-profile/pages/SchoolProfilePage";
import { CampusListPage } from "@/features/campuses/pages/CampusListPage";
import { CampusCreatePage } from "@/features/campuses/pages/CampusCreatePage";
import { CampusDetailsPage } from "@/features/campuses/pages/CampusDetailsPage";
import { CampusEditPage } from "@/features/campuses/pages/CampusEditPage";
import {
  AcademicYearListPage,
  AcademicYearCreatePage,
  AcademicYearDetailsPage,
  AcademicYearEditPage,
} from "@/features/academic-years/academicYears";
import {
  SchoolClassListPage,
  SchoolClassCreatePage,
  SchoolClassDetailsPage,
  SchoolClassEditPage,
} from "@/features/classes/schoolClasses";
import {
  ClassSectionListPage,
  ClassSectionCreatePage,
  ClassSectionDetailsPage,
  ClassSectionEditPage,
} from "@/features/sections/classSections";
import { SubjectListPage, SubjectCreatePage, SubjectDetailsPage, SubjectEditPage, ClassSubjectsPage } from "@/features/subjects/subjects";
import { DepartmentListPage, DepartmentCreatePage, DepartmentDetailsPage, DepartmentEditPage } from "@/features/departments/pages/DepartmentPages";
import { HouseListPage, HouseCreatePage, HouseDetailsPage, HouseEditPage } from "@/features/houses/pages/HousePages";
import { CalendarListPage, CalendarCreatePage, CalendarDetailsPage, CalendarEditPage } from "@/features/school-calendar/pages/CalendarPages";
import { UnauthorizedPage } from "@/pages/UnauthorizedPage";
import { NotFoundPage } from "@/pages/NotFoundPage";
export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout />}>
          <Route
            element={<PermissionRoute permission="School.ViewDashboard" />}
          >
            <Route path="/" element={<SchoolDashboardPage />} />
            <Route path="/dashboard" element={<SchoolDashboardPage />} />
          </Route>
          <Route element={<PermissionRoute permission="School.View" />}>
            <Route path="/settings/school" element={<SchoolProfilePage />} />
          </Route>
          <Route element={<PermissionRoute permission="Campus.View" />}>
            <Route path="/campuses" element={<CampusListPage />} />
            <Route path="/campuses/:id" element={<CampusDetailsPage />} />
            <Route path="/campuses/:id/edit" element={<CampusEditPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Campus.Create" />}>
            <Route path="/campuses/new" element={<CampusCreatePage />} />
          </Route>
          <Route element={<PermissionRoute permission="AcademicYear.View" />}>
            <Route path="/academic-years" element={<AcademicYearListPage />} />
            <Route
              path="/academic-years/:id"
              element={<AcademicYearDetailsPage />}
            />
            <Route
              path="/academic-years/:id/edit"
              element={<AcademicYearEditPage />}
            />
          </Route>
          <Route element={<PermissionRoute permission="AcademicYear.Create" />}>
            <Route
              path="/academic-years/new"
              element={<AcademicYearCreatePage />}
            />
          </Route>
          <Route element={<PermissionRoute permission="Class.View" />}>
            <Route path="/classes" element={<SchoolClassListPage />} />
            <Route path="/classes/:id" element={<SchoolClassDetailsPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Class.Edit" />}>
            <Route path="/classes/:id/edit" element={<SchoolClassEditPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Class.Create" />}>
            <Route path="/classes/new" element={<SchoolClassCreatePage />} />
          </Route>
          <Route element={<PermissionRoute permission="Section.View" />}>
            <Route path="/sections" element={<ClassSectionListPage />} />
            <Route path="/sections/:id" element={<ClassSectionDetailsPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Section.Create" />}>
            <Route
              path="/classes/:classId/sections/new"
              element={<ClassSectionCreatePage />}
            />
          </Route>
          <Route element={<PermissionRoute permission="Section.Edit" />}>
            <Route
              path="/sections/:id/edit"
              element={<ClassSectionEditPage />}
            />
          </Route>
          <Route element={<PermissionRoute permission="Subject.View" />}>
            <Route path="/subjects" element={<SubjectListPage />} />
            <Route path="/subjects/:id" element={<SubjectDetailsPage />} />
            <Route path="/classes/:classId/subjects" element={<ClassSubjectsPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Subject.Create" />}>
            <Route path="/subjects/new" element={<SubjectCreatePage />} />
          </Route>
          <Route element={<PermissionRoute permission="Subject.Edit" />}>
            <Route path="/subjects/:id/edit" element={<SubjectEditPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Department.View" />}>
            <Route path="/departments" element={<DepartmentListPage />} />
            <Route path="/departments/:id" element={<DepartmentDetailsPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Department.Create" />}>
            <Route path="/departments/new" element={<DepartmentCreatePage />} />
          </Route>
          <Route element={<PermissionRoute permission="Department.Edit" />}>
            <Route path="/departments/:id/edit" element={<DepartmentEditPage />} />
          </Route>
          <Route element={<PermissionRoute permission="House.View" />}>
            <Route path="/houses" element={<HouseListPage />} />
            <Route path="/houses/:id" element={<HouseDetailsPage />} />
          </Route>
          <Route element={<PermissionRoute permission="House.Create" />}>
            <Route path="/houses/new" element={<HouseCreatePage />} />
          </Route>
          <Route element={<PermissionRoute permission="House.Edit" />}>
            <Route path="/houses/:id/edit" element={<HouseEditPage />} />
          </Route>
          <Route element={<PermissionRoute permission="Calendar.View" />}><Route path="/calendar" element={<CalendarListPage/>}/><Route path="/calendar/:id" element={<CalendarDetailsPage/>}/></Route>
          <Route element={<PermissionRoute permission="Calendar.Create" />}><Route path="/calendar/new" element={<CalendarCreatePage/>}/></Route>
          <Route element={<PermissionRoute permission="Calendar.Edit" />}><Route path="/calendar/:id/edit" element={<CalendarEditPage/>}/></Route>
          <Route
            element={<PermissionRoute permission="Platform.ViewDashboard" />}
          >
            <Route
              path="/platform/dashboard"
              element={<PlatformDashboardPage />}
            />
          </Route>
        </Route>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/unauthorized" element={<UnauthorizedPage />} />
        <Route path="/not-found" element={<NotFoundPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
