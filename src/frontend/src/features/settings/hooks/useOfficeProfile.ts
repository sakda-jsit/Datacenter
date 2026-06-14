import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { officeProfileApi, type OfficeProfile } from '../services/officeProfileApi'

const KEY = ['office-profile'] as const

export function useOfficeProfile() {
  return useQuery({ queryKey: KEY, queryFn: officeProfileApi.get })
}

export function useSaveOfficeProfile() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: OfficeProfile) => officeProfileApi.save(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
