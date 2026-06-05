import { useQuery } from '@tanstack/react-query'
import { stockApi } from '../services/stockApi'

export function useStockItems(companyId: number, enabled = true) {
  return useQuery({
    queryKey: ['stock-items', companyId],
    queryFn: () => stockApi.items(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useStockValuation(companyId: number, fiscalYear: number, enabled = true) {
  return useQuery({
    queryKey: ['stock-valuation', companyId, fiscalYear],
    queryFn: () => stockApi.valuation(companyId, fiscalYear),
    enabled: enabled && companyId > 0,
  })
}
