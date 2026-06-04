// NOTE: API serializes enums as integers (no JsonStringEnumConverter). contractType เป็น number.

/** ตรงกับ Domain.Enums.LeaseContractType */
export const ContractType = {
  HirePurchase: 0,
  Loan: 1,
} as const

export type LeaseContractType = (typeof ContractType)[keyof typeof ContractType]

export const CONTRACT_TYPE_LABEL: Record<number, string> = {
  0: 'เช่าซื้อ/ลีสซิ่ง',
  1: 'เงินกู้',
}

export const CONTRACT_TYPE_OPTIONS: { value: LeaseContractType; label: string }[] = [
  { value: 0, label: 'เช่าซื้อ/ลีสซิ่ง' },
  { value: 1, label: 'เงินกู้' },
]

export interface LeaseContractListItem {
  id: number
  contractType: number
  contractNo: string
  assetName: string
  assetCode?: string
  lessor?: string
  firstInstallmentDate: string
  numberOfPeriods: number
  financedPrincipal: number
  installmentAmount: number
  isActive: boolean
}

export interface LeaseContract {
  id: number
  clientCompanyId: number
  contractType: number
  contractNo: string
  assetName: string
  assetCode?: string
  lessor?: string
  contractDate: string
  firstInstallmentDate: string
  numberOfPeriods: number
  paymentsPerYear: number
  cashPrice: number
  downPayment: number
  financedPrincipal: number
  installmentAmount: number
  vatPerPeriod: number
  liabilityAccountId: number
  liabilityAccountCode?: string
  deferredInterestAccountId?: number
  deferredInterestAccountCode?: string
  inputVatUndueAccountId?: number
  inputVatUndueAccountCode?: string
  interestExpenseAccountId: number
  interestExpenseAccountCode?: string
  notes?: string
  attachmentPath?: string
  isActive: boolean
  totalInterest: number
  totalVat: number
  grossLiabilityTotal: number
  effectiveRatePerPeriod: number
}

export interface LeaseSchedulePeriod {
  periodNo: number
  dueDate: string
  installment: number
  principal: number
  interest: number
  vat: number
  grossInstallment: number
  closingNetPrincipal: number
  closingDeferredInterest: number
  closingVatUndue: number
  closingGrossLiability: number
}

export interface LeaseAccountBreakdown {
  opening: number
  paidInYear: number
  closing: number
  currentPortion: number
  longTerm: number
}

export interface LeaseYearEndSummary {
  fiscalYear: number
  grossLiability: LeaseAccountBreakdown
  deferredInterest: LeaseAccountBreakdown
  vatUndue: LeaseAccountBreakdown
  netPrincipal: LeaseAccountBreakdown
  interestRecognizedInYear: number
}

export interface LeaseContractDetail {
  contract: LeaseContract
  yearEnd: LeaseYearEndSummary
  schedule: LeaseSchedulePeriod[]
}

export interface LeaseWorkpaperRow {
  contractId: number
  contractType: number
  contractNo: string
  assetName: string
  assetCode?: string
  lessor?: string
  grossLiability: LeaseAccountBreakdown
  deferredInterest: LeaseAccountBreakdown
  vatUndue: LeaseAccountBreakdown
  interestRecognizedInYear: number
}

export interface LeaseGlCompare {
  accountId: number
  accountCode: string
  accountName: string
  role: string
  scheduleClosing: number
  glClosing: number
  diff: number
}

export interface LeaseWorkpaper {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  rows: LeaseWorkpaperRow[]
  glComparison: LeaseGlCompare[]
  totalGrossLiabilityClosing: number
  totalDeferredInterestClosing: number
  totalVatUndueClosing: number
  totalInterestRecognized: number
  hasDifference: boolean
}

/** ฟิลด์ที่แก้ไขได้ (ตรงกับ LeaseContractInput) */
export interface LeaseContractInput {
  contractType: number
  contractNo: string
  assetName: string
  assetCode?: string | null
  lessor?: string | null
  contractDate: string
  firstInstallmentDate: string
  numberOfPeriods: number
  paymentsPerYear: number
  cashPrice: number
  downPayment: number
  financedPrincipal: number
  installmentAmount: number
  vatPerPeriod: number
  liabilityAccountId: number
  deferredInterestAccountId?: number | null
  inputVatUndueAccountId?: number | null
  interestExpenseAccountId: number
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
}
