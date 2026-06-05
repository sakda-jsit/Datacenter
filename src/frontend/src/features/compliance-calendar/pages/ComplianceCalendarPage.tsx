import { useEffect, useState } from 'react'
import DataTable, { type DataTableColumn } from '../../../shared/components/table/DataTable'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import type { ClientListDto } from '../../clients/types/client.types'
import type { ExportSection } from '../../../shared/utils/exportTable'
import {
  useComplianceDashboard,
  useComplianceTasks,
  useGenerateTasks,
  useUpdateTaskStatus,
} from '../hooks/useCompliance'
import type { ComplianceTaskDto, ComplianceTaskStatus, MonthSummaryDto } from '../types/compliance.types'
import { STATUS_COLORS, STATUS_LABELS, TASK_TYPE_LABELS } from '../types/compliance.types'

const MONTH_NAMES = [
  '', 'ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.',
  'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.',
]

interface Props {
  clients: ClientListDto[]
}

export default function ComplianceCalendarPage({ clients }: Props) {
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(new Date().getFullYear())
  const [selectedMonth, setSelectedMonth] = useState<number | null>(null)
  const selectedClient = clients.find((client) => client.id === companyId)

  const { data: dashboard } = useComplianceDashboard(companyId, year)
  const { data: tasks, isLoading: tasksLoading } = useComplianceTasks(companyId, year, selectedMonth ?? undefined)

  const generate = useGenerateTasks()
  const updateStatus = useUpdateTaskStatus()

  useEffect(() => {
    setSelectedMonth(null)
  }, [companyId])

  async function handleGenerate(month: number) {
    await generate.mutateAsync({ clientCompanyId: companyId, year, month })
  }

  const taskColumns: DataTableColumn<ComplianceTaskDto>[] = [
    {
      key: 'month',
      header: 'เดือน',
      render: (task) => <span className="text-gray-600">{MONTH_NAMES[task.month]}</span>,
      sortValue: (task) => task.month,
      sortable: true,
      headerClassName: 'w-16',
    },
    {
      key: 'taskType',
      header: 'ประเภทงาน',
      render: (task) => <span className="text-gray-800">{task.taskTypeName}</span>,
      sortValue: (task) => task.taskTypeName,
      sortable: true,
    },
    {
      key: 'dueDate',
      header: 'ครบกำหนด',
      render: (task) => <DueDate task={task} />,
      sortValue: (task) => task.dueDate,
      sortable: true,
      headerClassName: 'w-32',
    },
    {
      key: 'status',
      header: 'สถานะ',
      render: (task) => (
        <span className={`rounded-full px-2 py-0.5 text-xs ${STATUS_COLORS[task.status]}`}>
          {STATUS_LABELS[task.status]}
        </span>
      ),
      sortValue: (task) => task.status,
      sortable: true,
      headerClassName: 'w-28',
    },
    {
      key: 'assignedUser',
      header: 'ผู้รับผิดชอบ',
      render: (task) => <span className="text-xs text-gray-500">{task.assignedUserName ?? '—'}</span>,
      sortValue: (task) => task.assignedUserName ?? '',
      sortable: true,
      headerClassName: 'w-32',
    },
    {
      key: 'note',
      header: 'หมายเหตุ',
      render: (task) => <span className="text-xs text-gray-500">{task.note ?? ''}</span>,
      sortValue: (task) => task.note ?? '',
      sortable: true,
    },
    {
      key: 'actions',
      header: '',
      render: (task) => (
        <StatusButtons
          status={task.status}
          onChange={(status) => updateStatus.mutate({ taskId: task.id, status })}
        />
      ),
      headerClassName: 'w-36',
    },
  ]

  return (
    <div className="space-y-4">
      {/* Header controls */}
      <Card className="flex flex-wrap items-center gap-4 p-4">
        <div>
          <p className="text-xs font-medium text-gray-500">บริษัทลูกค้า</p>
          <p className="text-sm font-semibold text-slate-800">
            {selectedClient ? selectedClient.name : 'เลือกบริษัทที่ header'}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-gray-700">ปี</label>
          <input
            type="number" min={2020} max={2100}
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            className="border border-gray-300 rounded px-3 py-2 text-sm w-24 focus:outline-none focus:ring-2 focus:ring-slate-400"
          />
        </div>
        {dashboard && dashboard.totalOverdue > 0 && (
          <span className="ml-auto bg-red-100 text-red-700 text-sm font-medium px-3 py-1 rounded-full">
            เกินกำหนด {dashboard.totalOverdue} รายการ
          </span>
        )}
      </Card>

      {companyId === 0 && (
        <Card>
          <StateMessage centered>เลือกบริษัทที่ header เพื่อดูปฏิทิน Compliance</StateMessage>
        </Card>
      )}

      {companyId > 0 && (
        <>
          {/* Upcoming due soon */}
          {dashboard && dashboard.upcomingDueSoon.length > 0 && (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
              <p className="text-sm font-semibold text-amber-800 mb-2">
                ครบกำหนดใน 7 วัน ({dashboard.upcomingDueSoon.length} รายการ)
              </p>
              <div className="flex flex-wrap gap-2">
                {dashboard.upcomingDueSoon.map(t => (
                  <span key={t.id} className="text-xs bg-amber-100 text-amber-700 px-2 py-1 rounded">
                    {MONTH_NAMES[t.month]} — {TASK_TYPE_LABELS[t.taskType]}
                    {' '}(ครบ {new Date(t.dueDate).toLocaleDateString('th-TH')})
                  </span>
                ))}
              </div>
            </div>
          )}

          {/* Month grid */}
          <div className="grid grid-cols-3 md:grid-cols-4 xl:grid-cols-6 gap-3">
            {Array.from({ length: 12 }, (_, i) => i + 1).map(m => {
              const summary = dashboard?.months.find(s => s.month === m)
              const isSelected = selectedMonth === m
              return (
                <MonthCard
                  key={m}
                  month={m}
                  summary={summary}
                  isSelected={isSelected}
                  onSelect={() => setSelectedMonth(isSelected ? null : m)}
                  onGenerate={() => handleGenerate(m)}
                  generating={generate.isPending}
                />
              )
            })}
          </div>

          {/* Task list */}
          <Card className="overflow-hidden">
            <div className="px-5 py-3 bg-slate-700 text-white flex items-center justify-between">
              <div className="flex items-center gap-3">
                <p className="font-semibold text-sm">
                  {selectedMonth
                    ? `รายการงาน — ${MONTH_NAMES[selectedMonth]} ${year}`
                    : `รายการงานทั้งปี ${year}`}
                </p>
                {tasksLoading && <span className="text-xs opacity-70">กำลังโหลด...</span>}
              </div>
              {tasks && tasks.length > 0 && (
                <ExportMenu
                  meta={{ title: `ปฏิทินงาน (Compliance) ปี ${year}`, fileName: `compliance-${year}` }}
                  getSections={(): ExportSection[] => [{
                    name: 'รายการงาน',
                    columns: [
                      { key: 'clientName', header: 'บริษัท' },
                      { key: 'taskTypeName', header: 'ประเภทงาน' },
                      { key: 'dueDate', header: 'ครบกำหนด', value: (t) => t.dueDate?.slice(0, 10) ?? '' },
                      { key: 'statusName', header: 'สถานะ' },
                      { key: 'assignedUserName', header: 'ผู้รับผิดชอบ', value: (t) => t.assignedUserName ?? '' },
                      { key: 'note', header: 'หมายเหตุ', value: (t) => t.note ?? '' },
                    ],
                    rows: tasks,
                  }]}
                />
              )}
            </div>
            {tasks && tasks.length === 0 && (
              <StateMessage centered>ยังไม่มีรายการงาน — กดปุ่ม "สร้าง" บนการ์ดเดือน</StateMessage>
            )}
            {tasks && tasks.length > 0 && (
              <div className="[&>div]:rounded-none [&>div]:shadow-none">
                <DataTable
                  rows={tasks}
                  columns={taskColumns}
                  getRowKey={(task) => task.id}
                  defaultSortKey="dueDate"
                  rowClassName={(task) => (task.isOverdue ? 'bg-red-50' : '')}
                />
              </div>
            )}
          </Card>
        </>
      )}
    </div>
  )
}

function MonthCard({
  month, summary, isSelected, onSelect, onGenerate, generating,
}: {
  month: number
  summary?: MonthSummaryDto
  isSelected: boolean
  onSelect: () => void
  onGenerate: () => void
  generating: boolean
}) {
  const hasData = summary && summary.total > 0
  const allDone = hasData && summary.completed === summary.total
  const hasOverdue = hasData && summary.overdue > 0

  let borderColor = 'border-gray-200'
  if (isSelected) borderColor = 'border-slate-600'
  else if (hasOverdue) borderColor = 'border-red-300'
  else if (allDone) borderColor = 'border-green-300'

  return (
    <Card
      className={`bg-white rounded-lg border-2 ${borderColor} p-3 cursor-pointer hover:shadow-md transition-shadow`}
      onClick={onSelect}
    >
      <div className="flex items-center justify-between mb-2">
        <span className="font-semibold text-slate-700">{MONTH_NAMES[month]}</span>
        {!hasData && (
          <Button
            type="button"
            variant="ghost"
            onClick={(e) => { e.stopPropagation(); onGenerate() }}
            disabled={generating}
            className="px-1.5 py-0.5 text-xs"
          >
            สร้าง
          </Button>
        )}
      </div>
      {hasData ? (
        <div className="space-y-1">
          <ProgressBar completed={summary.completed} total={summary.total} />
          <div className="flex gap-1 flex-wrap mt-1">
            {summary.completed > 0 && <Pill color="green">{summary.completed} เสร็จ</Pill>}
            {summary.inProgress > 0 && <Pill color="blue">{summary.inProgress} ดำเนินการ</Pill>}
            {summary.pending > 0 && <Pill color="gray">{summary.pending} รอ</Pill>}
            {summary.overdue > 0 && <Pill color="red">{summary.overdue} เกินกำหนด</Pill>}
          </div>
        </div>
      ) : (
        <p className="text-xs text-gray-400">ยังไม่มีรายการ</p>
      )}
    </Card>
  )
}

function ProgressBar({ completed, total }: { completed: number; total: number }) {
  const pct = total > 0 ? Math.round((completed / total) * 100) : 0
  return (
    <div className="w-full bg-gray-200 rounded-full h-1.5">
      <div className="bg-green-500 h-1.5 rounded-full transition-all" style={{ width: `${pct}%` }} />
    </div>
  )
}

function Pill({ color, children }: { color: string; children: React.ReactNode }) {
  const colors: Record<string, string> = {
    green: 'bg-green-100 text-green-700',
    blue: 'bg-blue-100 text-blue-700',
    gray: 'bg-gray-100 text-gray-600',
    red: 'bg-red-100 text-red-700',
  }
  return (
    <span className={`text-xs px-1.5 py-0.5 rounded ${colors[color]}`}>{children}</span>
  )
}

function DueDate({ task }: { task: ComplianceTaskDto }) {
  const due = new Date(task.dueDate)
  const dueFmt = due.toLocaleDateString('th-TH', { day: 'numeric', month: 'short', year: '2-digit' })
  const isOverdue = task.isOverdue

  return (
    <span className={`font-mono text-xs ${isOverdue ? 'font-semibold text-red-600' : 'text-gray-600'}`}>
      {dueFmt}
      {isOverdue && ' ⚠'}
    </span>
  )
}

function StatusButtons({
  status,
  onChange,
}: {
  status: ComplianceTaskStatus
  onChange: (s: ComplianceTaskStatus) => void
}) {
  if (status === 2) {
    return (
      <Button variant="ghost" onClick={() => onChange(0)} className="px-2 py-0.5 text-xs text-gray-500">
        ยกเลิก
      </Button>
    )
  }
  return (
    <div className="flex gap-1">
      {status !== 1 && (
        <Button
          type="button"
          variant="ghost"
          onClick={() => onChange(1)}
          className="bg-blue-50 px-2 py-0.5 text-xs text-blue-600 hover:bg-blue-100"
        >
          เริ่ม
        </Button>
      )}
      <Button
        type="button"
        variant="ghost"
        onClick={() => onChange(2)}
        className="bg-green-50 px-2 py-0.5 text-xs text-green-700 hover:bg-green-100"
      >
        เสร็จ
      </Button>
    </div>
  )
}
