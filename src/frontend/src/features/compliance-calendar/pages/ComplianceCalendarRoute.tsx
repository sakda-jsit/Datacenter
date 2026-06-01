import { useClientList } from '../../clients/hooks/useClients'
import ComplianceCalendarPage from './ComplianceCalendarPage'

/**
 * Container ของหน้า Compliance Calendar
 * ทำหน้าที่ดึงรายการบริษัทลูกค้าแล้วส่งให้ ComplianceCalendarPage ใช้เลือกบริษัท
 */
export default function ComplianceCalendarRoute() {
  const { data, isLoading } = useClientList({ pageNumber: 1, pageSize: 200 })

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow p-10 text-center text-gray-400">
        กำลังโหลดรายการบริษัท...
      </div>
    )
  }

  return <ComplianceCalendarPage clients={data?.items ?? []} />
}
