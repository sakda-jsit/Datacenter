import apiClient from '../../../shared/services/apiClient'

// โปรไฟล์สำนักงานบัญชี — ค่ากลางของระบบ (ตั้งครั้งเดียว ใช้ทุกบริษัท)
export interface OfficeProfile {
  officeName: string
  taxId?: string | null
  branchCode?: string | null
  address?: string | null
  phone?: string | null
}

export const officeProfileApi = {
  get: () => apiClient.get<OfficeProfile>('/office-profile').then((r) => r.data),
  save: (data: OfficeProfile) => apiClient.put<OfficeProfile>('/office-profile', data).then((r) => r.data),
}
