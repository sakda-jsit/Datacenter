import apiClient from '../../../shared/services/apiClient'
import type { PayrollRateConfig, PayrollRateConfigInput } from '../../payroll/types/payroll.types'

// อัตราเงินสมทบ ปกส./กองทุนทดแทน — ค่ากลางของระบบ (ไม่แยกบริษัท), effective-dated
export const payrollRatesApi = {
  list: () => apiClient.get<PayrollRateConfig[]>('/payroll-rates').then((r) => r.data),
  create: (data: PayrollRateConfigInput) =>
    apiClient.post<{ id: number }>('/payroll-rates', data).then((r) => r.data),
  update: (id: number, data: PayrollRateConfigInput) =>
    apiClient.put(`/payroll-rates/${id}`, data).then((r) => r.data),
  remove: (id: number) => apiClient.delete(`/payroll-rates/${id}`).then((r) => r.data),
}
