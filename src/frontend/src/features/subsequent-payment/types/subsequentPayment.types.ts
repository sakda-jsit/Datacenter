// Subsequent Payment Check (RPT-019) — docs/17
// อ่าน GLJNLIT ปีถัดไป "สด" จาก Express → หลักฐานประกอบเท่านั้น ไม่รวมยอดปีปิดงบ

export type SubsequentPaymentStatus = 'paid' | 'partial' | 'unpaid' | 'unmatched'

export interface SubsequentPaymentDetail {
  voucher: string
  date?: string | null
  description: string
  amount: number
}

export interface SubsequentPaymentRow {
  accountId: number
  accountCode: string
  accountName: string
  yearEndPayable: number
  subsequentPaid: number
  remaining: number
  status: SubsequentPaymentStatus
  payments: SubsequentPaymentDetail[]
}

export interface SubsequentPaymentReport {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  subsequentYear: number
  expressAvailable: boolean
  checkedAt: string
  rows: SubsequentPaymentRow[]
  totalYearEndPayable: number
  totalSubsequentPaid: number
  totalRemaining: number
  paidCount: number
  partialCount: number
  unpaidCount: number
}
