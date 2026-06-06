import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { payrollApi } from '../services/payrollApi'
import type { EmployeeInput, PayrollRateConfigInput } from '../types/payroll.types'

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

// ── อัตรา ปกส./กองทุนทดแทน ─────────────────────────────────────────────────────
export function usePayrollConfigs(companyId: number) {
  return useQuery({
    queryKey: ['payroll-config', companyId],
    queryFn: () => payrollApi.configs(companyId),
    enabled: companyId > 0,
  })
}

export function useSavePayrollConfig(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { id: number | null; data: PayrollRateConfigInput }) =>
      vars.id ? payrollApi.updateConfig(vars.id, companyId, vars.data) : payrollApi.createConfig(companyId, vars.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-config', companyId] }),
  })
}

export function useDeletePayrollConfig(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => payrollApi.deleteConfig(id, companyId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll-config', companyId] }),
  })
}
