import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Button from '../../../shared/components/ui/Button'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Pagination from '../../../shared/components/ui/Pagination'
import SearchInput from '../../../shared/components/ui/SearchInput'
import StateMessage from '../../../shared/components/ui/StateMessage'
import ClientTable from '../components/ClientTable'
import { useClientList, useDeactivateClient } from '../hooks/useClients'

export default function ClientListPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 20

  const { data, isLoading, isError } = useClientList({ pageNumber: page, pageSize, search: search || undefined })
  const deactivate = useDeactivateClient()

  function handleDeactivate(id: number, name: string) {
    if (!confirm(`ยืนยันการปิดใช้งานลูกค้า "${name}" ?`)) return
    deactivate.mutate(id)
  }

  return (
    <div>
      <PageHeader
        title="จัดการลูกค้า"
        action={(
          <Button type="button" onClick={() => navigate('/clients/new')}>
            + เพิ่มลูกค้าใหม่
          </Button>
        )}
      />

      <div className="mb-4">
        <SearchInput
          placeholder="ค้นหาชื่อบริษัทหรือเลขภาษี..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1) }}
          className="w-80"
        />
      </div>

      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาดในการโหลดข้อมูล</StateMessage>}

      {data && (
        <>
          <ClientTable
            clients={data.items}
            deactivatePending={deactivate.isPending}
            onDeactivate={handleDeactivate}
          />

          <Pagination
            page={page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  )
}
