// ดอกเบี้ยรับเงินให้กู้ (Interest Income) — req v11 docs/13. enum ไม่มีในโมดูลนี้

export interface InterestLoanListItem {
  id: number
  name: string
  reference?: string | null
  annualRatePct: number
  isActive: boolean
}

export interface LoanMovement {
  date: string
  amount: number
  description?: string | null
}

export interface LoanMovementInput {
  date: string
  amount: number
  description?: string | null
}

export interface InterestLoan {
  id: number
  clientCompanyId: number
  name: string
  reference?: string | null
  annualRatePct: number
  sbtRatePct: number
  localTaxPctOfSbt: number
  dayCountBasis: number
  interestReceivableAccountId: number
  interestReceivableAccountCode?: string | null
  interestIncomeAccountId: number
  interestIncomeAccountCode?: string | null
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
  movements: LoanMovement[]
}

export interface InterestLoanInput {
  name: string
  reference?: string | null
  annualRatePct: number
  sbtRatePct: number
  localTaxPctOfSbt: number
  dayCountBasis: number
  interestReceivableAccountId: number
  interestIncomeAccountId: number
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
  movements: LoanMovementInput[]
}

export interface InterestSegment {
  fromDate: string
  toDate: string
  balance: number
  days: number
  interest: number
}

export interface InterestAsOf {
  openingBalance: number
  closingBalance: number
  interestForYear: number
  sbt: number
  localTax: number
  totalTax: number
}

export interface InterestLoanDetail {
  item: InterestLoan
  asOf: InterestAsOf
  segments: InterestSegment[]
}

export interface InterestWorkpaperRow {
  id: number
  name: string
  reference?: string | null
  annualRatePct: number
  openingBalance: number
  closingBalance: number
  interestForYear: number
  sbt: number
  localTax: number
}

export interface InterestGlCompare {
  accountId: number
  accountCode: string
  accountName: string
  scheduleInterest: number
  glMovement: number
  diff: number
}

export interface InterestWorkpaper {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  rows: InterestWorkpaperRow[]
  glComparison: InterestGlCompare[]
  totalInterest: number
  totalSbt: number
  totalLocalTax: number
  hasDifference: boolean
}
