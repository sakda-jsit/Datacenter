import apiClient from '../../../shared/services/apiClient'
import type {
  EmployeeDetail,
  EmployeeInput,
  EmployeeListItem,
  PayrollAccountMapping,
  PayrollItemInput,
  PayrollRunDetail,
  PayrollRunListItem,
  PayrollYearSummary,
  PayrollPosting,
  SsoFiling,
} from '../types/payroll.types'

type MappingInput = { accountCode: string; role: number; department?: string | null; note?: string }

export const payrollApi = {
  employees: (clientCompanyId: number, includeResigned = true) =>
    apiClient
      .get<EmployeeListItem[]>('/payroll/employees', { params: { clientCompanyId, includeResigned } })
      .then((r) => r.data),

  employee: (id: number, clientCompanyId: number) =>
    apiClient
      .get<EmployeeDetail>(`/payroll/employees/${id}`, { params: { clientCompanyId } })
      .then((r) => r.data),

  createEmployee: (clientCompanyId: number, data: EmployeeInput) =>
    apiClient
      .post<{ id: number }>('/payroll/employees', data, { params: { clientCompanyId } })
      .then((r) => r.data),

  updateEmployee: (id: number, clientCompanyId: number, data: EmployeeInput) =>
    apiClient.put(`/payroll/employees/${id}`, data, { params: { clientCompanyId } }).then((r) => r.data),

  deleteEmployee: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/payroll/employees/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  // เอกสาร (PDPA)
  uploadDocument: (
    employeeId: number,
    clientCompanyId: number,
    docType: number,
    file: File,
    effectiveDate?: string | null,
    note?: string,
  ) => {
    const form = new FormData()
    form.append('file', file)
    return apiClient
      .post<{ id: number }>(`/payroll/employees/${employeeId}/documents`, form, {
        params: { clientCompanyId, docType, effectiveDate: effectiveDate || undefined, note: note || undefined },
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },

  downloadDocument: (docId: number, clientCompanyId: number) =>
    apiClient
      .get(`/payroll/documents/${docId}`, { params: { clientCompanyId }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  deleteDocument: (docId: number, clientCompanyId: number) =>
    apiClient.delete(`/payroll/documents/${docId}`, { params: { clientCompanyId } }).then((r) => r.data),

  // แจ้งเข้า-ออก ปกส.
  createEnrollment: (
    clientCompanyId: number,
    body: { employeeId: number; type: number; eventDate: string; note?: string },
  ) =>
    apiClient
      .post<{ id: number }>('/payroll/sso-enrollments', body, { params: { clientCompanyId } })
      .then((r) => r.data),

  updateEnrollment: (
    id: number,
    clientCompanyId: number,
    body: { submittedDate?: string | null; status: number; proofDocumentId?: number | null; note?: string },
  ) =>
    apiClient
      .put(`/payroll/sso-enrollments/${id}`, body, { params: { clientCompanyId } })
      .then((r) => r.data),

  // งวดเงินเดือนรายเดือน
  runs: (clientCompanyId: number, year?: number) =>
    apiClient
      .get<PayrollRunListItem[]>('/payroll/runs', { params: { clientCompanyId, year } })
      .then((r) => r.data),

  run: (id: number, clientCompanyId: number) =>
    apiClient
      .get<PayrollRunDetail>(`/payroll/runs/${id}`, { params: { clientCompanyId } })
      .then((r) => r.data),

  yearSummary: (clientCompanyId: number, year: number) =>
    apiClient
      .get<PayrollYearSummary>('/payroll/year-summary', { params: { clientCompanyId, year } })
      .then((r) => r.data),

  downloadPnd1kExcel: (clientCompanyId: number, year: number) =>
    apiClient
      .get('/payroll/pnd1k/excel', { params: { clientCompanyId, year }, responseType: 'blob' })
      .then((r) => r.data as Blob),
  downloadPnd1kPdf: (clientCompanyId: number, year: number) =>
    apiClient
      .get('/payroll/pnd1k/pdf', { params: { clientCompanyId, year }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  // 50 ทวิ เงินเดือน (ภ.ง.ด.1ก) — employeeIds ว่าง = ทุกคน
  download50TawiPdf: (clientCompanyId: number, year: number, employeeIds?: number[]) =>
    apiClient
      .get('/payroll/50tawi/pdf', { params: { clientCompanyId, year, employeeIds }, responseType: 'blob' })
      .then((r) => r.data as Blob),
  tawiImages: (clientCompanyId: number, year: number, employeeIds?: number[]) =>
    apiClient
      .get<string[]>('/payroll/50tawi/images', { params: { clientCompanyId, year, employeeIds } })
      .then((r) => r.data),

  // กท.20ก (แบบแสดงเงินค่าจ้างประจำปี กองทุนเงินทดแทน)
  kt20Images: (clientCompanyId: number, year: number) =>
    apiClient
      .get<string[]>('/payroll/kt20/images', { params: { clientCompanyId, year } })
      .then((r) => r.data),
  downloadKt20Pdf: (clientCompanyId: number, year: number) =>
    apiClient
      .get('/payroll/kt20/pdf', { params: { clientCompanyId, year }, responseType: 'blob' })
      .then((r) => r.data as Blob),
  downloadKt20Excel: (clientCompanyId: number, year: number) =>
    apiClient
      .get('/payroll/kt20/excel', { params: { clientCompanyId, year }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  createRun: (clientCompanyId: number, year: number, month: number) =>
    apiClient
      .post<{ id: number }>('/payroll/runs', { year, month }, { params: { clientCompanyId } })
      .then((r) => r.data),

  saveItems: (runId: number, clientCompanyId: number, items: PayrollItemInput[]) =>
    apiClient
      .put(`/payroll/runs/${runId}/items`, items, { params: { clientCompanyId } })
      .then((r) => r.data),

  setRunStatus: (runId: number, clientCompanyId: number, status: number) =>
    apiClient
      .put(`/payroll/runs/${runId}/status`, { status }, { params: { clientCompanyId } })
      .then((r) => r.data),

  deleteRun: (runId: number, clientCompanyId: number) =>
    apiClient.delete(`/payroll/runs/${runId}`, { params: { clientCompanyId } }).then((r) => r.data),

  // ใบสำคัญลงบัญชี + กระทบยอด
  posting: (runId: number, clientCompanyId: number) =>
    apiClient
      .get<PayrollPosting>(`/payroll/runs/${runId}/posting`, { params: { clientCompanyId } })
      .then((r) => r.data),

  // สปส.1-10
  ssoFiling: (runId: number, clientCompanyId: number) =>
    apiClient
      .get<SsoFiling>(`/payroll/runs/${runId}/sso-filing`, { params: { clientCompanyId } })
      .then((r) => r.data),

  downloadSsoExcel: (runId: number, clientCompanyId: number) =>
    apiClient
      .get(`/payroll/runs/${runId}/sso-filing/excel`, { params: { clientCompanyId }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  downloadSsoPdf: (runId: number, clientCompanyId: number) =>
    apiClient
      .get(`/payroll/runs/${runId}/sso-filing/pdf`, { params: { clientCompanyId }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  downloadTemplate: (runId: number, clientCompanyId: number) =>
    apiClient
      .get(`/payroll/runs/${runId}/template`, { params: { clientCompanyId }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  importRun: (runId: number, clientCompanyId: number, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return apiClient
      .post<{ updated: number }>(`/payroll/runs/${runId}/import`, form, {
        params: { clientCompanyId },
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },

  // แมพบัญชีเงินเดือน (Express → ฝ่าย)
  accountMappings: (clientCompanyId: number) =>
    apiClient
      .get<PayrollAccountMapping[]>('/payroll/account-mappings', { params: { clientCompanyId } })
      .then((r) => r.data),
  createAccountMapping: (clientCompanyId: number, data: MappingInput) =>
    apiClient
      .post<{ id: number }>('/payroll/account-mappings', data, { params: { clientCompanyId } })
      .then((r) => r.data),
  updateAccountMapping: (id: number, clientCompanyId: number, data: MappingInput) =>
    apiClient.put(`/payroll/account-mappings/${id}`, data, { params: { clientCompanyId } }).then((r) => r.data),
  deleteAccountMapping: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/payroll/account-mappings/${id}`, { params: { clientCompanyId } }).then((r) => r.data),
}
