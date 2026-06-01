import { useAuth } from '../../hooks/useAuth'
import { useClientList } from '../../../features/clients/hooks/useClients'
import SearchableSelect from '../ui/SearchableSelect'
import { useCurrentCompany } from '../../hooks/useCurrentCompany'

interface TopBarProps {
  onOpenMenu: () => void
}

export default function TopBar({ onOpenMenu }: TopBarProps) {
  const { user, logout } = useAuth()
  const { companyId, selectCompany } = useCurrentCompany()
  const { data: clientsData } = useClientList({ pageNumber: 1, pageSize: 200 })
  const displayName = user?.displayName || user?.username || 'ผู้ใช้งาน'
  const initial = displayName.trim().charAt(0).toUpperCase() || 'U'
  const clients = clientsData?.items ?? []

  return (
    <header className="sticky top-0 z-20 flex min-h-[66px] items-center justify-between gap-4 border-b border-sky-100 bg-white/80 px-4 py-3 backdrop-blur-xl sm:px-6 lg:px-8">
      <div className="flex min-w-0 items-center gap-3">
        <button
          type="button"
          onClick={onOpenMenu}
          className="grid h-10 w-10 place-items-center rounded-xl border border-slate-200 bg-white text-slate-700 shadow-sm md:hidden"
          aria-label="เปิดเมนู"
        >
          ☰
        </button>
        <div className="min-w-0">
          <SearchableSelect
            value={companyId}
            onChange={(value) => selectCompany(Number(value))}
            placeholder="เลือกบริษัทลูกค้า"
            searchPlaceholder="ค้นหาชื่อลูกค้าหรือรหัส..."
            className="w-[240px] sm:w-[360px]"
            options={[
              { value: 0, label: 'เลือกบริษัทลูกค้า' },
              ...clients
                .filter((client) => client.isActive)
                .map((client) => ({
                  value: client.id,
                  label: client.name,
                  searchText: `${client.code} ${client.name} ${client.taxId}`,
                })),
            ]}
          />
        </div>
      </div>
      <div className="flex min-w-0 items-center gap-3">
        <span className="grid h-10 w-10 flex-none place-items-center rounded-xl bg-gradient-to-br from-sky-400 to-sky-300 text-sm font-extrabold text-white shadow-[0_10px_22px_rgba(56,189,248,0.22)]">
          {initial}
        </span>
        <span className="hidden min-w-0 flex-col leading-tight sm:flex">
          <strong className="max-w-[180px] truncate text-sm font-extrabold text-slate-900">{displayName}</strong>
          <small className="mt-0.5 max-w-[180px] truncate text-[11px] text-slate-500">Accounting Workspace</small>
        </span>
        <button
          onClick={logout}
          className="inline-flex h-9 items-center justify-center rounded-xl border border-slate-200 bg-white px-3 text-xs font-bold text-slate-600 transition hover:-translate-y-0.5 hover:border-red-200 hover:bg-red-50 hover:text-red-600"
        >
          Logout
        </button>
      </div>
    </header>
  )
}
