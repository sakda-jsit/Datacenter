export interface StockItem {
  id: number
  stockCode: string
  name: string
  itemType?: string
  groupCode?: string
  unit?: string
  onHandQty: number
  unitCost: number
  stockValue: number
}

export interface StockGroupSummary {
  groupCode: string
  count: number
  totalValue: number
}

export interface StockGlCompare {
  accountId: number
  accountCode: string
  accountName: string
  glBalance: number
}

export interface StockValuation {
  clientCompanyId: number
  clientName: string
  fiscalYear: number
  items: StockItem[]
  groups: StockGroupSummary[]
  glAccounts: StockGlCompare[]
  dataAsOf?: string
  totalStockValue: number
  totalGlBalance: number
  difference: number
  hasGlAccounts: boolean
}
