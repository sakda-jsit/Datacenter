export interface FsLineAccountDto {
  accountCode: string
  accountName: string
  netBalance: number
}

export interface FsLineDto {
  refCode: string
  lineName: string
  section: string
  sortOrder: number
  amount: number
  accounts: FsLineAccountDto[]
  noteNo?: string | null
}

export interface BalanceSheetDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  assets: FsLineDto[]
  liabilities: FsLineDto[]
  equity: FsLineDto[]
  totalAssets: number
  totalLiabilities: number
  totalEquity: number
  totalLiabilitiesAndEquity: number
  balanceDifference: number
}

export interface ProfitLossDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  monthFrom?: number
  monthTo?: number
  incomeLines: FsLineDto[]
  costOfGoods: FsLineDto
  expenseLines: FsLineDto[]
  financeCost: FsLineDto
  incomeTax: FsLineDto
  totalIncome: number
  totalExpenses: number
  grossProfit: number
  profitBeforeFinance: number
  profitBeforeTax: number
  netProfit: number
}

export interface UnmappedAccountDto {
  accountCode: string
  accountName: string
  netBalance: number
}

export interface UnmappedAccountsResult {
  fiscalYear: number
  mappedCount: number
  unmappedWithBalanceCount: number
  totalNet: number
  items: UnmappedAccountDto[]
}

export interface AccountMappingDto {
  accountCode: string
  accountName: string
  refCode: string
  lineName: string
  section: string
}

// ผังมาตรฐาน DBD (master taxonomy — single source ของ RefCode)
export interface StatementTaxonomyLine {
  refCode: string
  lineName: string
  section: string
  sortOrder: number
  mappedAccountCount: number
}

export interface StatementTaxonomy {
  clientCompanyId: number
  lines: StatementTaxonomyLine[]
  totalLines: number
  usedLines: number
  mappedAccounts: number
}

export interface FsExternalInputDto {
  id: number
  fiscalYear: number
  refCode: string
  amount: number
  note?: string
}

export interface EquityComponentDto {
  refCode: string
  name: string
  opening: number
  netProfit: number
  otherChange: number
  closing: number
}

export interface EquityChangesDto {
  clientCompanyId: number
  clientName: string
  fiscalYear: number
  components: EquityComponentDto[]
  balanceSheetEquity: number
  totalOpening: number
  totalNetProfit: number
  totalOtherChange: number
  totalClosing: number
  tiesToBalanceSheet: boolean
}

// ── NOTE2 (หมายเหตุประกอบงบการเงิน) ──────────────────────────────────────────

export interface NoteNarrativeDto {
  noteNo: string
  title: string
  body: string
  sortOrder: number
  effectiveYear: number
  isCompanyOverride: boolean
}

export interface NoteRowDto {
  label: string
  currentYear: number
  priorYear: number
}

export interface NoteScheduleDto {
  noteNo: string
  title: string
  sortOrder: number
  rows: NoteRowDto[]
  totalCurrent: number
  totalPrior: number
}

export interface NoteMovementRowDto {
  label: string
  opening: number
  additions: number
  disposals: number
  closing: number
}

export interface NoteMovementDto {
  noteNo: string
  title: string
  sortOrder: number
  costRows: NoteMovementRowDto[]
  costTotal: NoteMovementRowDto
  accumRows: NoteMovementRowDto[]
  accumTotal: NoteMovementRowDto
  netOpening: number
  netClosing: number
  chargeForYear: number
}

export interface NoteCostOfSalesDto {
  noteNo: string
  title: string
  sortOrder: number
  openingInventoryCurrent: number
  openingInventoryPrior: number
  components: NoteRowDto[]
  closingInventoryCurrent: number
  closingInventoryPrior: number
  totalCurrent: number
  totalPrior: number
}

export interface NotesToFsDto {
  clientCompanyId: number
  clientName: string
  taxId: string
  address?: string
  fiscalYear: number
  priorYear: number
  periodLabel: string
  narratives: NoteNarrativeDto[]
  schedules: NoteScheduleDto[]
  movements: NoteMovementDto[]
  costOfSales?: NoteCostOfSalesDto | null
}

export interface NoteTemplateSectionDto {
  id: number
  clientCompanyId?: number | null
  effectiveYear: number
  noteKey: string
  title: string
  bodyText: string
  sortOrder: number
}
