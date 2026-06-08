import apiClient from '../../../shared/services/apiClient'
import type { SubsequentPaymentReport } from '../types/subsequentPayment.types'

export const subsequentPaymentApi = {
  check: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<SubsequentPaymentReport>('/subsequent-payment/check', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),
}
