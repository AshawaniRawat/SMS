import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { httpClient } from "@/api/httpClient";
import { session } from "@/auth/session";
import type { ApiResponse } from "@/types/api";
type C = { id: string; academicYearId: string; name: string; academicYearName: string };
type S = {
  id: string;
  academicYearId: string;
  academicYearName: string;
  academicYearCode: string;
  name: string;
  code: string;
  shortName: string | null;
  type: number;
  category: number | null;
  displayOrder: number;
  creditHours: number | null;
  weeklyPeriods: number | null;
  status: 1 | 2 | 3 | 4;
  assignedClassCount: number;
  description: string | null;
};
type P<T> = { items: T[]; hasPreviousPage: boolean; hasNextPage: boolean };
type Y = { id: string; name: string; status: number };
type M = {
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  type: number;
  isMandatory: boolean;
  displayOrder: number;
  status: number;
};
const d = <T,>(r: { data: ApiResponse<T> }) => r.data.data!;
const statuses = {
  1: "Draft",
  2: "Active",
  3: "Inactive",
  4: "Archived",
} as const;
const types = {
  1: "Core",
  2: "Elective",
  3: "Co-Curricular",
  4: "Language",
  5: "Practical",
  6: "Activity",
  99: "Other",
} as const;
const cats = {
  1: "Language",
  2: "Mathematics",
  3: "Science",
  4: "Social Science",
  5: "Computer",
  6: "Commerce",
  7: "Humanities",
  8: "Physical Education",
  9: "Arts",
  10: "Vocational",
  99: "Other",
} as const;
export function SubjectListPage() {
  const [year, setYear] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [type, setType] = useState("");
  const [page, setPage] = useState(1);
  const ys = useQuery({
    queryKey: ["academic-years", "subject-filter"],
    queryFn: async () =>
      d(
        await httpClient.get<ApiResponse<P<Y>>>("/v1/academic-years", {
          params: { pageSize: 100 },
        }),
      ),
  });
  const q = useQuery({
    queryKey: ["subjects", { year, search, status, type, page }],
    enabled: !!year,
    queryFn: async () =>
      d(
        await httpClient.get<ApiResponse<P<S>>>("/v1/subjects", {
          params: {
            academicYearId: year,
            search,
            status: status || undefined,
            type: type || undefined,
            pageNumber: page,
            pageSize: 20,
          },
        }),
      ),
  });
  return (
    <div className="space-y-6">
      <header className="flex justify-between">
        <h1 className="text-3xl font-semibold">Subjects</h1>
        {session.hasPermission("Subject.Create") && (
          <Link
            className="rounded bg-primary p-2 text-primary-foreground"
            to="/subjects/new"
          >
            Create Subject
          </Link>
        )}
      </header>
      <div className="grid gap-3 md:grid-cols-4">
        <select
          aria-label="Academic year"
          value={year}
          onChange={(e) => setYear(e.target.value)}
          className="rounded border p-2"
        >
          <option value="">Select academic year</option>
          {ys.data?.items.map((y) => (
            <option key={y.id} value={y.id}>
              {y.name}
            </option>
          ))}
        </select>
        <input
          aria-label="Search subjects"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search subjects"
          className="rounded border p-2"
        />
        <select
          aria-label="Status"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          className="rounded border p-2"
        >
          <option value="">All statuses</option>
          {Object.entries(statuses).map(([v, n]) => (
            <option key={v} value={v}>
              {n}
            </option>
          ))}
        </select>
        <select
          aria-label="Type"
          value={type}
          onChange={(e) => setType(e.target.value)}
          className="rounded border p-2"
        >
          <option value="">All types</option>
          {Object.entries(types).map(([v, n]) => (
            <option key={v} value={v}>
              {n}
            </option>
          ))}
        </select>
      </div>
      {!year ? (
        <div className="rounded-xl border border-dashed p-12 text-center">
          Select an academic year to view subjects.
        </div>
      ) : q.isLoading ? (
        <div className="h-64 animate-pulse rounded bg-card" />
      ) : q.isError ? (
        <div role="alert">Unable to load subjects.</div>
      ) : !q.data!.items.length ? (
        <div className="rounded-xl border border-dashed p-12 text-center">
          No subjects have been configured for this academic year.
        </div>
      ) : (
        <>
          <div className="overflow-x-auto rounded-xl border bg-card">
            <table className="w-full min-w-[850px] text-left">
              <thead>
                <tr>
                  {[
                    "Order",
                    "Subject",
                    "Code",
                    "Type",
                    "Category",
                    "Academic Year",
                    "Classes",
                    "Status",
                  ].map((x) => (
                    <th className="p-4" key={x}>
                      {x}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {q.data!.items.map((s) => (
                  <tr className="border-t" key={s.id}>
                    <td className="p-4">{s.displayOrder}</td>
                    <td>
                      <Link to={`/subjects/${s.id}`} className="text-primary">
                        {s.name}
                      </Link>
                    </td>
                    <td>{s.code}</td>
                    <td>{types[s.type as keyof typeof types]}</td>
                    <td>
                      {s.category ? cats[s.category as keyof typeof cats] : "—"}
                    </td>
                    <td>{s.academicYearName}</td>
                    <td>{s.assignedClassCount}</td>
                    <td>{statuses[s.status]}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button
            disabled={!q.data!.hasPreviousPage}
            onClick={() => setPage((x) => x - 1)}
          >
            Previous
          </button>
          <button
            className="ml-5"
            disabled={!q.data!.hasNextPage}
            onClick={() => setPage((x) => x + 1)}
          >
            Next
          </button>
        </>
      )}
    </div>
  );
}
const schema = z.object({
  academicYearId: z.string().min(1),
  name: z.string().trim().min(1).max(150),
  code: z
    .string()
    .trim()
    .min(1)
    .max(50)
    .regex(/^[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*$/),
  shortName: z.string().max(50),
  type: z.string().min(1),
  category: z.string(),
  displayOrder: z.coerce.number().min(0),
  creditHours: z.union([z.literal(""), z.coerce.number().positive().max(100)]),
  weeklyPeriods: z.union([
    z.literal(""),
    z.coerce.number().int().min(1).max(50),
  ]),
  description: z.string().max(1000),
});
type F = z.input<typeof schema>;
function Form({ item }: { item?: S }) {
  const nav = useNavigate();
  const ys = useQuery({
    queryKey: ["academic-years", "subject-form"],
    queryFn: async () =>
      d(
        await httpClient.get<ApiResponse<P<Y>>>("/v1/academic-years", {
          params: { pageSize: 100 },
        }),
      ),
  });
  const f = useForm<F>({
    resolver: zodResolver(schema),
    defaultValues: {
      academicYearId: item?.academicYearId ?? "",
      name: item?.name ?? "",
      code: item?.code ?? "",
      shortName: item?.shortName ?? "",
      type: item?.type.toString() ?? "1",
      category: item?.category?.toString() ?? "",
      displayOrder: item?.displayOrder ?? 0,
      creditHours: item?.creditHours ?? "",
      weeklyPeriods: item?.weeklyPeriods ?? "",
      description: item?.description ?? "",
    },
  });
  const m = useMutation({
    mutationFn: async (v: F) => {
      const b = {
        ...v,
        code: v.code.toUpperCase(),
        type: Number(v.type),
        category: v.category ? Number(v.category) : null,
        shortName: v.shortName || null,
        creditHours: v.creditHours === "" ? null : Number(v.creditHours),
        weeklyPeriods: v.weeklyPeriods === "" ? null : Number(v.weeklyPeriods),
        description: v.description || null,
      };
      return d(
        await (item
          ? httpClient.put<ApiResponse<S>>(`/v1/subjects/${item.id}`, {
              name: b.name,
              shortName: b.shortName,
              type: b.type,
              category: b.category,
              displayOrder: b.displayOrder,
              creditHours: b.creditHours,
              weeklyPeriods: b.weeklyPeriods,
              description: b.description,
            })
          : httpClient.post<ApiResponse<S>>("/v1/subjects", b)),
      );
    },
    onSuccess: (x) => nav(`/subjects/${x.id}`),
  });
  return (
    <form
      onSubmit={f.handleSubmit((v) => m.mutate(v))}
      className="grid gap-5 rounded-xl border bg-card p-6 md:grid-cols-2"
    >
      <label>
        Academic Year
        <select
          disabled={!!item}
          {...f.register("academicYearId")}
          className="mt-2 w-full rounded border p-2"
        >
          <option value="">Select year</option>
          {ys.data?.items
            .filter((y) => y.status < 3)
            .map((y) => (
              <option key={y.id} value={y.id}>
                {y.name}
              </option>
            ))}
        </select>
      </label>
      {(
        [
          "name",
          "code",
          "shortName",
          "displayOrder",
          "creditHours",
          "weeklyPeriods",
        ] as const
      ).map((n) => (
        <label key={n}>
          {n}
          <input
            disabled={!!item && n === "code"}
            type={
              ["displayOrder", "creditHours", "weeklyPeriods"].includes(n)
                ? "number"
                : "text"
            }
            {...f.register(n)}
            className="mt-2 w-full rounded border p-2"
          />
          <span className="text-xs text-red-600">
            {f.formState.errors[n]?.message}
          </span>
        </label>
      ))}
      <label>
        Type
        <select
          {...f.register("type")}
          className="mt-2 w-full rounded border p-2"
        >
          {Object.entries(types).map(([v, n]) => (
            <option key={v} value={v}>
              {n}
            </option>
          ))}
        </select>
      </label>
      <label>
        Category
        <select
          {...f.register("category")}
          className="mt-2 w-full rounded border p-2"
        >
          <option value="">None</option>
          {Object.entries(cats).map(([v, n]) => (
            <option key={v} value={v}>
              {n}
            </option>
          ))}
        </select>
      </label>
      <label className="md:col-span-2">
        Description
        <textarea
          {...f.register("description")}
          className="mt-2 w-full rounded border p-2"
        />
      </label>
      <button className="rounded bg-primary p-2 text-primary-foreground">
        Save
      </button>
      {m.isError && <p role="alert">Unable to save subject.</p>}
    </form>
  );
}
export const SubjectCreatePage = () => (
  <div className="space-y-6">
    <h1 className="text-3xl font-semibold">Create Subject</h1>
    <Form />
  </div>
);
export function SubjectEditPage() {
  const { id = "" } = useParams();
  const q = useQuery({
    queryKey: ["subject", id],
    queryFn: async () =>
      d(await httpClient.get<ApiResponse<S>>(`/v1/subjects/${id}`)),
  });
  return q.isLoading ? (
    <div>Loading...</div>
  ) : q.isError ? (
    <div>Subject not found.</div>
  ) : (
    <div className="space-y-6">
      <h1 className="text-3xl font-semibold">Edit Subject</h1>
      <Form item={q.data} />
    </div>
  );
}
export function SubjectDetailsPage() {
  const { id = "" } = useParams();
  const qc = useQueryClient();
  const q = useQuery({
    queryKey: ["subject", id],
    queryFn: async () =>
      d(await httpClient.get<ApiResponse<S>>(`/v1/subjects/${id}`)),
  });
  const m = useMutation({
    mutationFn: async (a: string) =>
      d(await httpClient.post<ApiResponse<S>>(`/v1/subjects/${id}/${a}`)),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["subject", id] }),
  });
  if (q.isLoading) return <div>Loading...</div>;
  if (q.isError) return <div>Subject not found.</div>;
  const s = q.data!;
  const run = (a: string) => window.confirm(`Confirm ${a}?`) && m.mutate(a);
  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-semibold">{s.name}</h1>
      <p>
        {s.code} · {s.academicYearName} · {statuses[s.status]}
      </p>
      <div className="flex gap-4">
        {s.status !== 4 && session.hasPermission("Subject.Edit") && (
          <Link to={`/subjects/${id}/edit`}>Edit</Link>
        )}
        {(s.status === 1 || s.status === 3) &&
          session.hasPermission("Subject.Activate") && (
            <button onClick={() => run("activate")}>Activate</button>
          )}
        {s.status === 2 && session.hasPermission("Subject.Deactivate") && (
          <button onClick={() => run("deactivate")}>Deactivate</button>
        )}
        {(s.status === 1 || s.status === 3) &&
          session.hasPermission("Subject.Archive") && (
            <button onClick={() => run("archive")}>Archive</button>
          )}
      </div>
      {m.isError && (
        <p role="alert">
          Unable to update subject. Remove class assignments before archiving.
        </p>
      )}
      <section className="rounded-xl border bg-card p-6">
        <p>Type: {types[s.type as keyof typeof types]}</p>
        <p>
          Category:{" "}
          {s.category ? cats[s.category as keyof typeof cats] : "None"}
        </p>
        <p>
          Credits: {s.creditHours ?? "Not set"} · Weekly periods:{" "}
          {s.weeklyPeriods ?? "Not set"}
        </p>
        <p>{s.description ?? "No description."}</p>
      </section>
    </div>
  );
}
export function ClassSubjectsPage() {
  const { classId = "" } = useParams();
  const qc = useQueryClient();
  const parent = useQuery({
    queryKey: ["school-class", classId],
    queryFn: async () => d(await httpClient.get<ApiResponse<C>>(`/v1/classes/${classId}`)),
  });
  const q = useQuery({
    queryKey: ["class-subjects", classId],
    queryFn: async () =>
      d(
        await httpClient.get<ApiResponse<M[]>>(
          `/v1/classes/${classId}/subjects`,
        ),
      ),
  });
  const available = useQuery({
    queryKey: ["subjects", "mapping", parent.data?.academicYearId],
    enabled: !!parent.data?.academicYearId,
    queryFn: async () =>
      d(
        await httpClient.get<ApiResponse<P<S>>>("/v1/subjects", {
          params: { academicYearId: parent.data!.academicYearId, pageSize: 100 },
        }),
      ),
  });
  const [items, setItems] = useState<M[]>([]);
  const save = useMutation({
    mutationFn: () =>
      httpClient.put(`/v1/classes/${classId}/subjects`, {
        items: items.map((x) => ({
          subjectId: x.subjectId,
          isMandatory: x.isMandatory,
          displayOrder: x.displayOrder,
        })),
      }),
    onSuccess: () =>
      qc.invalidateQueries({ queryKey: ["class-subjects", classId] }),
  });
  const shown = items.length ? items : (q.data ?? []);
  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-semibold">Class Subjects</h1>
      {parent.data && <p>{parent.data.name} · {parent.data.academicYearName}</p>}
      {q.isLoading ? (
        <div>Loading...</div>
      ) : q.isError ? (
        <div>Unable to load subjects.</div>
      ) : !shown.length ? (
        <p>No subjects have been assigned to this class.</p>
      ) : (
        shown.map((x, i) => (
          <div key={x.subjectId} className="flex gap-4 rounded border p-3">
            <span>
              {x.subjectName} ({x.subjectCode})
            </span>
            <label>
              <input
                type="checkbox"
                checked={x.isMandatory}
                onChange={(e) =>
                  setItems(
                    shown.map((y) =>
                      y.subjectId === x.subjectId
                        ? { ...y, isMandatory: e.target.checked }
                        : y,
                    ),
                  )
                }
              />{" "}
              Mandatory
            </label>
            <input
              aria-label={`Order ${x.subjectName}`}
              type="number"
              value={x.displayOrder}
              onChange={(e) =>
                setItems(
                  shown.map((y, j) =>
                    j === i
                      ? { ...y, displayOrder: Number(e.target.value) }
                      : y,
                  ),
                )
              }
            />
          </div>
        ))
      )}
      {session.hasPermission("Subject.ManageClassMapping") && (
        <>
          {available.data?.items.filter((s) => !shown.some((x) => x.subjectId === s.id)).map((s) => (
            <button
              key={s.id}
              onClick={() =>
                setItems([
                  ...shown,
                  {
                    subjectId: s.id,
                    subjectName: s.name,
                    subjectCode: s.code,
                    type: s.type,
                    isMandatory: true,
                    displayOrder: shown.length,
                    status: s.status,
                  },
                ])
              }
            >
              Add {s.name}
            </button>
          ))}
          <button onClick={() => save.mutate()}>Save mappings</button>
        </>
      )}
    </div>
  );
}
