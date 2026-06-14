import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { corporateTaxApi } from '../services/corporateTaxApi'
import type {
  CompanyDefaultSignersInput,
  CompanyYearSignersInput,
  TaxComputationInput,
} from '../types/corporateTax.types'

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

export function useCompanySigners(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: [KEY, 'signers', companyId, year],
    queryFn: () => corporateTaxApi.getSigners(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

export function useSignerAssignments(filters: { search?: string; auditorId?: number; bookkeeperId?: number }) {
  return useQuery({
    queryKey: [KEY, 'signer-assignments', filters],
    queryFn: () => corporateTaxApi.getSignerAssignments(filters),
  })
}

export function useSetDefaultSigners() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { companyId: number; data: CompanyDefaultSignersInput }) =>
      corporateTaxApi.setDefaultSigners(vars.companyId, vars.data),
    onSuccess: (_res, vars) => {
      qc.invalidateQueries({ queryKey: [KEY, 'signers', vars.companyId] })
      qc.invalidateQueries({ queryKey: [KEY, 'signer-assignments'] })
    },
  })
}

export function useSaveYearSigners() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { companyId: number; year: number; data: CompanyYearSignersInput }) =>
      corporateTaxApi.saveYearSigners(vars.companyId, vars.year, vars.data),
    // เลื่อนเป็น default ด้วย → กระทบทุกปี → invalidate ทั้งบริษัท
    onSuccess: (_res, vars) =>
      qc.invalidateQueries({ queryKey: [KEY, 'signers', vars.companyId] }),
  })
}
