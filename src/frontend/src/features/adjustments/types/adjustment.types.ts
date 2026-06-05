// NOTE: API serializes enums as integers (no JsonStringEnumConverter — closing-period /
// compliance pages rely on numeric status). So sourceType/accountType are numbers here,
// and POST/PUT must send numeric sourceType.

/** ตรงกับ Domain.Enums.AdjustmentSourceType */
export const SourceType = {
  Manual: 0,
  Leasing: 1,
  Loan: 2,
  Tax: 3,
  Other: 4,
  FixedAsset: 5,
} as const

export type AdjustmentSourceType = (typeof SourceType)[keyof typeof SourceType]

export const SOURCE_TYPE_LABEL: Record<number, string> = {
  0: 'ปรับปรุงด้วยมือ',
  1: 'สัญญาเช่า',
  2: 'เงินกู้',
  3: 'ภาษี',
  4: 'อื่น ๆ',
  5: 'สินทรัพย์ถาวร',
}

export const SOURCE_TYPE_OPTIONS: { value: AdjustmentSourceType; label: string }[] = [
  { value: 0, label: 'ปรับปรุงด้วยมือ' },
  { value: 1, label: 'สัญญาเช่า' },
  { value: 2, label: 'เงินกู้' },
  { value: 3, label: 'ภาษี' },
  { value: 4, label: 'อื่น ๆ' },
]

/** ตรงกับ Domain.Enums.AccountType (Asset=1..Expense=5) */
export const ACCOUNT_TYPE_LABEL: Record<number, string> = {
  1: 'สินทรัพย์',
  2: 'หนี้สิน',
  3: 'ส่วนของเจ้าของ',
  4: 'รายได้',
  5: 'ค่าใช้จ่าย',
}

export interface AdjustmentLineDto {
  id: number
  accountId: number
  accountCode: string
  accountName: string
  debitAmount: number
  creditAmount: number
  description?: string
}

export interface AdjustmentEntryDto {
  id: number
  clientCompanyId: number
  fiscalYear: number
  documentNo: string
  entryDate: string
  sourceType: number
  reference?: string
  reason: string
  attachmentPath?: string
  createdBy?: string
  createdAt: string
  lines: AdjustmentLineDto[]
  totalDebit: number
  totalCredit: number
}

export interface AdjustmentLineInput {
  accountId: number
  debitAmount: number
  creditAmount: number
  description?: string | null
}

export interface CreateAdjustmentEntryInput {
  clientCompanyId: number
  fiscalYear: number
  entryDate: string
  sourceType: number
  reference?: string | null
  reason: string
  attachmentPath?: string | null
  lines: AdjustmentLineInput[]
}

export interface UpdateAdjustmentEntryInput extends Omit<CreateAdjustmentEntryInput, 'fiscalYear'> {
  id: number
}

// ── Adjusted trial balance ────────────────────────────────────────────────────

export interface AdjustedTrialBalanceRowDto {
  accountId: number
  accountCode: string
  accountName: string
  accountType: number
  level: number
  parentCode?: string
  beginDebit: number
  beginCredit: number
  movementDebit: number
  movementCredit: number
  balanceBeforeDebit: number
  balanceBeforeCredit: number
  adjustmentDebit: number
  adjustmentCredit: number
  finalDebit: number
  finalCredit: number
}

export interface AdjustedTrialBalanceReportDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  rows: AdjustedTrialBalanceRowDto[]
  totalBalanceBeforeDebit: number
  totalBalanceBeforeCredit: number
  totalAdjustmentDebit: number
  totalAdjustmentCredit: number
  totalFinalDebit: number
  totalFinalCredit: number
  balancedBefore: boolean
  adjustmentsBalanced: boolean
  balancedAfter: boolean
}
