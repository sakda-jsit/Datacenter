// NOTE: API serializes enums as integers. vatType เป็น number (1=ขาย, 2=ซื้อ)

export const VatType = {
  Output: 1, // ภาษีขาย
  Input: 2, // ภาษีซื้อ
} as const

export type VatTypeValue = (typeof VatType)[keyof typeof VatType]

export const VAT_TYPE_LABEL: Record<number, string> = {
  1: 'ภาษีขาย',
  2: 'ภาษีซื้อ',
}

export interface VatMonthly {
  month: number
  outputBase: number
  outputVat: number
  outputZeroRated: number
  outputCount: number
  inputBase: number
  inputVat: number
  inputCount: number
  netVat: number
}

export interface VatReport {
  clientCompanyId: number
  clientName: string
  year: number
  months: VatMonthly[]
  totalOutputBase: number
  totalOutputVat: number
  totalOutputZeroRated: number
  totalInputBase: number
  totalInputVat: number
  totalNetVat: number
  totalOutputCount: number
  totalInputCount: number
  dataAsOf?: string
}

export interface VatEntryItem {
  id: number
  vatType: number
  taxPeriod: string
  documentDate?: string
  documentNo: string
  referenceNo?: string
  description?: string
  counterpartyTaxId?: string
  counterpartyPrefix?: string
  baseAmount: number
  vatAmount: number
  zeroRatedAmount: number
  isLate: boolean
}

export interface Pp30BranchRow {
  departmentCode: string
  branchNo: string
  isHeadOffice: boolean
  totalSales: number
  zeroRatedSales: number
  exemptSales: number
  eligiblePurchase: number
  outputVat: number
  inputVat: number
}

export interface Pp30Branches {
  companyName: string
  taxId: string
  year: number
  month: number
  isMultiBranch: boolean
  branches: Pp30BranchRow[]
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
