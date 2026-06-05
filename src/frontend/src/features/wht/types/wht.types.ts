// NOTE: API serializes enums as integers. formType เป็น number (3=ภ.ง.ด.3, 53=ภ.ง.ด.53)

export const WhtForm = {
  Pnd3: 3, // บุคคลธรรมดา
  Pnd53: 53, // นิติบุคคล
} as const

export const WHT_FORM_LABEL: Record<number, string> = {
  3: 'ภ.ง.ด.3',
  53: 'ภ.ง.ด.53',
}

export interface WhtMonthly {
  month: number
  pnd3Base: number
  pnd3Tax: number
  pnd3Count: number
  pnd53Base: number
  pnd53Tax: number
  pnd53Count: number
  totalTax: number
}

export interface WhtReport {
  clientCompanyId: number
  clientName: string
  year: number
  months: WhtMonthly[]
  totalPnd3Base: number
  totalPnd3Tax: number
  totalPnd3Count: number
  totalPnd53Base: number
  totalPnd53Tax: number
  totalPnd53Count: number
  totalTax: number
  dataAsOf?: string
}

export const WhtEmailStatus = {
  NotSent: 0,
  Sending: 1,
  Sent: 2,
  Failed: 3,
} as const

export const EMAIL_STATUS_LABEL: Record<number, string> = {
  0: 'ยังไม่ส่ง',
  1: 'กำลังส่ง',
  2: 'ส่งแล้ว',
  3: 'ส่งไม่สำเร็จ',
}

export const EMAIL_STATUS_CLASS: Record<number, string> = {
  0: 'bg-gray-100 text-gray-600',
  1: 'bg-sky-100 text-sky-700',
  2: 'bg-green-100 text-green-700',
  3: 'bg-red-100 text-red-700',
}

export interface WhtEntryItem {
  id: number
  formType: number
  taxPeriod: string
  withholdDate?: string
  documentNo: string
  payeeName?: string
  payeePrefix?: string
  payeeTaxId?: string
  incomeType?: string
  baseAmount: number
  taxRate: number
  taxAmount: number
  isLate: boolean
  payeeEmail?: string
  emailStatus: number
  emailSentAt?: string
  emailSentBy?: string
  emailError?: string
}

export interface WhtSendResult {
  payeeTaxId: string
  payeeName?: string
  email?: string
  success: boolean
  entryCount: number
  error?: string
}

export const MONTH_LABEL: Record<number, string> = {
  1: 'มกราคม',
  2: 'กุมภาพันธ์',
  3: 'มีนาคม',
  4: 'เมษายน',
  5: 'พฤษภาคม',
  6: 'มิถุนายน',
  7: 'กรกฎาคม',
  8: 'สิงหาคม',
  9: 'กันยายน',
  10: 'ตุลาคม',
  11: 'พฤศจิกายน',
  12: 'ธันวาคม',
}
