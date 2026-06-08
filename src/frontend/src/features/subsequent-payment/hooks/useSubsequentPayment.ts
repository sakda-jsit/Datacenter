import { useQuery } from '@tanstack/react-query'
import { subsequentPaymentApi } from '../services/subsequentPaymentApi'

const keys = {
  check: (companyId: number, year: number) => ['subsequent-payment', companyId, year] as const,
}

export function useSubsequentPaymentCheck(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.check(companyId, year),
    queryFn: () => subsequentPaymentApi.check(companyId, year),
    enabled: enabled && companyId > 0,
  })
}
