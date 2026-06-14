import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { corporateTaxApi } from '../services/corporateTaxApi'
import type { TaxComputationInput } from '../types/corporateTax.types'

const KEY = 'corporate-tax'

export function useTaxComputation(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: [KEY, 'computation', companyId, year],
    queryFn: () => corporateTaxApi.getComputation(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

export function useSaveTaxComputation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { companyId: number; year: number; data: TaxComputationInput }) =>
      corporateTaxApi.saveComputation(vars.companyId, vars.year, vars.data),
    onSuccess: (_res, vars) => {
      qc.invalidateQueries({ queryKey: [KEY, 'computation', vars.companyId, vars.year] })
      // งบดุล/งบกำไรขาดทุนเปลี่ยนเพราะ X4 ถูก mirror ลง FsExternalInput
      qc.invalidateQueries({ queryKey: ['financial-statement'] })
    },
  })
}
