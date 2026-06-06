import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { payrollApi } from '../services/payrollApi'
import type { EmployeeInput, PayrollItemInput } from '../types/payroll.types'

const keys = {
  employees: (companyId: number, includeResigned: boolean) =>
    ['payroll-employees', companyId, includeResigned] as const,
  employee: (companyId: number, id: number) => ['payroll-employee', companyId, id] as const,
}

export function useEmployees(companyId: number, includeResigned = true) {
  return useQuery({
    queryKey: keys.employees(companyId, includeResigned),
    queryFn: () => payrollApi.employees(companyId, includeResigned),
    enabled: companyId > 0,
  })
}

export function useEmployee(companyId: number, id: number | null) {
  return useQuery({
    queryKey: keys.employee(companyId, id ?? 0),
    queryFn: () => payrollApi.employee(id!, companyId),
    enabled: companyId > 0 && !!id,
  })
}

export function useSaveEmployee(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { id: number | null; data: EmployeeInput }) =>
      vars.id
        ? payrollApi.updateEmployee(vars.id, companyId, vars.data)
        : payrollApi.createEmployee(companyId, vars.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-employees', companyId] }),
  })
}

export function useDeleteEmployee(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => payrollApi.deleteEmployee(id, companyId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-employees', companyId] }),
  })
}

function invalidateEmployee(companyId: number, employeeId: number) {
  return (qc: ReturnType<typeof useQueryClient>) => {
    qc.invalidateQueries({ queryKey: ['payroll-employee', companyId, employeeId] })
    qc.invalidateQueries({ queryKey: ['payroll-employees', companyId] })
  }
}

export function useUploadDocument(companyId: number, employeeId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { docType: number; file: File; effectiveDate?: string | null; note?: string }) =>
      payrollApi.uploadDocument(employeeId, companyId, vars.docType, vars.file, vars.effectiveDate, vars.note),
    onSuccess: () => invalidateEmployee(companyId, employeeId)(qc),
  })
}

export function useDeleteDocument(companyId: number, employeeId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (docId: number) => payrollApi.deleteDocument(docId, companyId),
    onSuccess: () => invalidateEmployee(companyId, employeeId)(qc),
  })
}

export function useCreateEnrollment(companyId: number, employeeId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { employeeId: number; type: number; eventDate: string; note?: string }) =>
      payrollApi.createEnrollment(companyId, body),
    onSuccess: () => invalidateEmployee(companyId, employeeId)(qc),
  })
}

export function useUpdateEnrollment(companyId: number, employeeId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: {
      id: number
      body: { submittedDate?: string | null; status: number; proofDocumentId?: number | null; note?: string }
    }) => payrollApi.updateEnrollment(vars.id, companyId, vars.body),
    onSuccess: () => invalidateEmployee(companyId, employeeId)(qc),
  })
}

// ── งวดเงินเดือน ────────────────────────────────────────────────────────────────
export function usePayrollRuns(companyId: number, year?: number) {
  return useQuery({
    queryKey: ['payroll-runs', companyId, year ?? 0],
    queryFn: () => payrollApi.runs(companyId, year),
    enabled: companyId > 0,
  })
}

export function usePayrollYearSummary(companyId: number, year: number) {
  return useQuery({
    queryKey: ['payroll-year-summary', companyId, year],
    queryFn: () => payrollApi.yearSummary(companyId, year),
    enabled: companyId > 0 && year > 0,
  })
}

export function usePayrollRun(companyId: number, runId: number | null) {
  return useQuery({
    queryKey: ['payroll-run', companyId, runId ?? 0],
    queryFn: () => payrollApi.run(runId!, companyId),
    enabled: companyId > 0 && !!runId,
  })
}

export function useCreatePayrollRun(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { year: number; month: number }) => payrollApi.createRun(companyId, vars.year, vars.month),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-runs', companyId] }),
  })
}

export function useSavePayrollItems(companyId: number, runId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (items: PayrollItemInput[]) => payrollApi.saveItems(runId, companyId, items),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['payroll-run', companyId, runId] })
      qc.invalidateQueries({ queryKey: ['payroll-runs', companyId] })
    },
  })
}

export function useSetRunStatus(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { runId: number; status: number }) => payrollApi.setRunStatus(vars.runId, companyId, vars.status),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-run', companyId] }),
  })
}

export function useDeletePayrollRun(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (runId: number) => payrollApi.deleteRun(runId, companyId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-runs', companyId] }),
  })
}

export function usePayrollAccountMappings(companyId: number) {
  return useQuery({
    queryKey: ['payroll-acct-map', companyId],
    queryFn: () => payrollApi.accountMappings(companyId),
    enabled: companyId > 0,
  })
}

export function useSaveAccountMapping(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { id: number | null; data: { accountCode: string; department: string; note?: string } }) =>
      vars.id ? payrollApi.updateAccountMapping(vars.id, companyId, vars.data) : payrollApi.createAccountMapping(companyId, vars.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-acct-map', companyId] }),
  })
}

export function useDeleteAccountMapping(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => payrollApi.deleteAccountMapping(id, companyId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-acct-map', companyId] }),
  })
}

export function useImportPayrollRun(companyId: number, runId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => payrollApi.importRun(runId, companyId, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['payroll-run', companyId, runId] })
      qc.invalidateQueries({ queryKey: ['payroll-runs', companyId] })
    },
  })
}
