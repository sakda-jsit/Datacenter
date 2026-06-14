// NOTE: API serialize enum เป็น integer — ใช้ numeric enum, POST ส่งตัวเลข

export const TaxRateScheme = {
  SmeTiered: 1, // SME ขั้นบันได 0/15/20
  Flat20: 2, // นิติบุคคลทั่วไป 20%
  Custom: 3, // กำหนดอัตราเอง
} as const

export const TAX_RATE_SCHEME_LABEL: Record<number, string> = {
  1: 'SME ขั้นบันได (0 / 15% / 20%)',
  2: 'นิติบุคคลทั่วไป 20%',
  3: 'กำหนดอัตราเอง',
}

export const TaxAdjustmentKind = {
  AddBack: 1, // บวกกลับ
  Deduction: 2, // หักออก
} as const

export interface TaxBracket {
  label: string
  base: number
  ratePct: number
  tax: number
}

export interface TaxComputationResult {
  netProfitBeforeTax: number
  addBackTotal: number
  deductionTotal: number
  adjustedProfit: number
  lossBroughtForward: number
  lossUsed: number
  netTaxableIncome: number
  brackets: TaxBracket[]
  taxAmount: number
  whtCredit: number
  netPayable: number
  lossCarriedForward: number
}

export interface TaxAdjustmentLine {
  id: number
  kind: number
  description: string
  amount: number
  sortOrder: number
}

export interface TaxComputation {
  clientCompanyId: number
  clientName: string
  fiscalYear: number
  rateScheme: number
  customRatePct?: number | null
  lossBroughtForward: number
  whtCredit: number
  note?: string | null
  lines: TaxAdjustmentLine[]
  result: TaxComputationResult
  hasProfitLoss: boolean
  warnings: string[]
}

export interface TaxAdjustmentLineInput {
  kind: number
  description: string
  amount: number
  sortOrder: number
}

export interface TaxComputationInput {
  rateScheme: number
  customRatePct?: number | null
  lossBroughtForward: number
  whtCredit: number
  note?: string | null
  lines: TaxAdjustmentLineInput[]
}

// ── ผู้ลงนาม: master (ทะเบียน) + ค่าเริ่มต้นบริษัท + override รายปี ──
export interface CompanySigners {
  clientCompanyId: number
  fiscalYear: number
  defaultAuditorId?: number | null
  defaultBookkeeperId?: number | null
  yearAuditorId?: number | null
  yearBookkeeperId?: number | null
  resolvedAuditorId?: number | null
  resolvedBookkeeperId?: number | null
  signDate?: string | null
  hasYearOverride: boolean
}

export interface CompanyDefaultSignersInput {
  auditorId?: number | null
  bookkeeperId?: number | null
}

// ── แมพบัญชี → บรรทัด CIT50 (รายการ 8) ──
export interface Cit50Line {
  code: string
  scheduleNo: number
  label: string
  isCatchAll: boolean
  isTotal: boolean
}
export interface Cit50AccountRow {
  accountCode: string
  accountName: string
  amount: number
  cit50LineCode?: string | null
}
export interface Cit50MappingView {
  lines: Cit50Line[]
  accounts: Cit50AccountRow[]
}
export interface Cit50MappingItem {
  accountCode: string
  accountName: string
  cit50LineCode?: string | null
}

export interface SignerAssignment {
  companyId: number
  companyName: string
  companyCode: string
  defaultAuditorId?: number | null
  defaultAuditorName?: string | null
  defaultBookkeeperId?: number | null
  defaultBookkeeperName?: string | null
  overrideYears: number
}

export interface CompanyYearSignersInput {
  auditorId?: number | null
  bookkeeperId?: number | null
  signDate?: string | null
}
