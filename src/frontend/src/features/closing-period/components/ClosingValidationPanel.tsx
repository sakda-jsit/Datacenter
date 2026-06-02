import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { monthName } from './MonthCard'
import type { ClosingValidationDto, ClosingValidationItemDto } from '../types/closingPeriod.types'

const SEVERITY_STYLE: Record<ClosingValidationItemDto['severity'], { icon: string; tone: string }> = {
  Error: { icon: '✕', tone: 'text-red-600' },
  Warning: { icon: '!', tone: 'text-amber-600' },
  Info: { icon: '–', tone: 'text-slate-400' },
}

interface Props {
  year: number
  month: number
  validation?: ClosingValidationDto
  loading: boolean
  isAdmin: boolean
  busy: boolean
  errorMessage?: string | null
  onClose: () => void
  onReopen: () => void
  onLock: () => void
}

export default function ClosingValidationPanel({
  year, month, validation, loading, isAdmin, busy, errorMessage,
  onClose, onReopen, onLock,
}: Props) {
  const status = validation?.currentStatus ?? 0
  const isOpen = status === 0
  const isClosed = status === 1
  const isLocked = status === 2

  function itemIcon(item: ClosingValidationItemDto) {
    if (item.passed && item.severity !== 'Info') return { icon: '✓', tone: 'text-green-600' }
    return SEVERITY_STYLE[item.severity]
  }

  return (
    <Card className="p-5">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">
          ตรวจสอบงวด {monthName(month)} {year}
        </h2>
      </div>

      {loading && <StateMessage>กำลังตรวจสอบ...</StateMessage>}

      {!loading && validation && (
        <>
          <ul className="mb-4 space-y-2">
            {validation.items.map((item) => {
              const { icon, tone } = itemIcon(item)
              return (
                <li key={item.code} className="flex items-start gap-2 text-sm">
                  <span className={`mt-0.5 font-bold ${tone}`}>{icon}</span>
                  <span className="flex-1">
                    <span className="text-slate-700">{item.label}</span>
                    {item.detail && <span className="block text-xs text-slate-400">{item.detail}</span>}
                  </span>
                </li>
              )
            })}
          </ul>

          {errorMessage && <StateMessage tone="error">{errorMessage}</StateMessage>}

          <div className="mt-4 flex flex-wrap gap-2 border-t border-slate-100 pt-4">
            {isOpen && (
              <Button onClick={onClose} disabled={busy || !validation.canClose}>
                {busy ? 'กำลังปิดงวด...' : 'ปิดงวด'}
              </Button>
            )}
            {isOpen && !validation.canClose && (
              <span className="self-center text-xs text-red-500">
                มีรายการที่ไม่ผ่าน ไม่สามารถปิดงวดได้
              </span>
            )}
            {isClosed && isAdmin && (
              <>
                <Button variant="secondary" onClick={onLock} disabled={busy}>ล็อกถาวร</Button>
                <Button variant="danger" onClick={onReopen} disabled={busy}>เปิดงวดใหม่</Button>
              </>
            )}
            {isLocked && isAdmin && (
              <Button variant="danger" onClick={onReopen} disabled={busy}>เปิดงวดใหม่</Button>
            )}
            {(isClosed || isLocked) && !isAdmin && (
              <span className="self-center text-xs text-slate-400">
                เฉพาะผู้ดูแลระบบ (Admin) เท่านั้นที่เปิด/ล็อกงวดได้
              </span>
            )}
          </div>
        </>
      )}
    </Card>
  )
}
