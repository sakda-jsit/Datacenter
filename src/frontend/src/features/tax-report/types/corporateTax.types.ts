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

// ── ผู้ตรวจสอบและรับรองบัญชี (ต่อรอบปี — เปลี่ยนได้รายปี) ──
export interface CompanyAuditor {
  clientCompanyId: number
  fiscalYear: number
  auditorName: string
  auditorLicenseNo?: string | null
  auditorTaxId?: string | null
  signDate?: string | null
  note?: string | null
  exists: boolean
}

export interface CompanyAuditorInput {
  auditorName: string
  auditorLicenseNo?: string | null
  auditorTaxId?: string | null
  signDate?: string | null
  note?: string | null
}
