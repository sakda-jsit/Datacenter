import apiClient from '../../../shared/services/apiClient'
import type { StockItem, StockValuation } from '../types/stock.types'

export const stockApi = {
  items: (clientCompanyId: number) =>
    apiClient.get<StockItem[]>('/stock', { params: { clientCompanyId } }).then((r) => r.data),

  valuation: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<StockValuation>('/stock/valuation', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),
}
