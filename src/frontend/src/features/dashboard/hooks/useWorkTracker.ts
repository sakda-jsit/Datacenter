import { useQuery } from '@tanstack/react-query'
import apiClient from '../../../shared/services/apiClient'
import type { WorkTrackerOverview } from '../types/workTracker.types'

export function useWorkTracker(year: number, month: number, enabled = true) {
  return useQuery({
    queryKey: ['dashboard', 'work-tracker', year, month],
    queryFn: () =>
      apiClient
        .get<WorkTrackerOverview>('/dashboard/work-tracker', { params: { year, month } })
        .then((r) => r.data),
    enabled,
  })
}
