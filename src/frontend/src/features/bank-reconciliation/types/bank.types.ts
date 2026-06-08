export interface BankAccount {
  id: number
  bankAccountCode: string
  bankName: string
  branch?: string
  accountNumber?: string
  glAccountCode?: string
  balanceForward: number
  currentBalance: number
  transactionCount: number
}

export interface BankBookRow {
  id: number
  transactionDate: string
  transactionType?: string
  chequeNo?: string
  counterpartyName?: string
  remark?: string
  deposit: number
  withdrawal: number
  balance: number
}

export interface BankBook {
  clientCompanyId: number
  clientName: string
  bankAccountCode: string
  bankName: string
  accountNumber?: string
  year: number
  openingBalance: number
  rows: BankBookRow[]
  totalDeposit: number
  totalWithdrawal: number
  closingBalance: number
  dataAsOf?: string
}

// ── Reconciliation (กระทบยอด statement) ───────────────────────────────────────
export interface StatementParsePreviewLine {
  date: string
  description?: string | null
  withdrawal: number
  deposit: number
  balance?: number | null
}

export interface StatementParsePreview {
  bankCode: string
  accountNo?: string | null
  periodStart?: string | null
  periodEnd?: string | null
  openingBalance: number
  closingBalance: number
  computedClosing: number
  balanceCheckPasses: boolean
  warning?: string | null
  expectedAccountNo?: string | null
  accountMatches?: boolean | null
  lines: StatementParsePreviewLine[]
}

export interface BankStatementImportListItem {
  id: number
  bankAccountId: number
  bankCode: string
  statementAccountNo?: string | null
  periodStart: string
  periodEnd: string
  openingBalance: number
  closingBalance: number
  parsedOk: boolean
  status: number
  lineCount: number
  matchedCount: number
  createdAt: string
  createdBy: string
  expectedAccountNo?: string | null
  accountMatches?: boolean | null
}

export interface ReconMatchedPair {
  statementLineId: number
  date: string
  description?: string | null
  amount: number
  isDeposit: boolean
  bankTransactionId: number
  bookDate: string
  bookCounterparty?: string | null
}

export interface ReconStatementLine {
  statementLineId: number
  date: string
  description?: string | null
  withdrawal: number
  deposit: number
  balance?: number | null
}

export interface ReconBookTxn {
  bankTransactionId: number
  date: string
  counterparty?: string | null
  remark?: string | null
  deposit: number
  withdrawal: number
}

export interface BankReconciliation {
  importId: number
  clientCompanyId: number
  clientName: string
  bankAccountId: number
  bankAccountCode: string
  bankName: string
  bankCode: string
  periodStart: string
  periodEnd: string
  statementOpeningBalance: number
  statementClosingBalance: number
  bookClosingBalance: number
  reconciledDifference: number
  isBalanced: boolean
  parsedOk: boolean
  matched: ReconMatchedPair[]
  unmatchedStatement: ReconStatementLine[]
  unmatchedBook: ReconBookTxn[]
  dataAsOf?: string | null
  matchedCount: number
  matchedAmount: number
  unmatchedStatementCount: number
  unmatchedBookCount: number
}
