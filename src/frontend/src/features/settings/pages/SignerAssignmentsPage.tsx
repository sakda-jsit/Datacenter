import { useEffect, useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useSignerAssignments, useSetDefaultSigners } from '../../tax-report/hooks/useCorporateTax'
import { useAuditors, useBookkeepers } from '../hooks/useAuditRegistry'

type Edit = { auditorId: number | ''; bookkeeperId: number | '' }

export default function SignerAssignmentsPage() {
  const [search, setSearch] = useState('')
  const [filterAuditor, setFilterAuditor] = useState<number | ''>('')
  const [filterBk, setFilterBk] = useState<number | ''>('')

  const filters = useMemo(() => ({
    search: search.trim() || undefined,
    auditorId: filterAuditor || undefined,
    bookkeeperId: filterBk || undefined,
  }), [search, filterAuditor, filterBk])

  const { data, isLoading, isError } = useSignerAssignments(filters)
  const { data: auditors } = useAuditors()
  const { data: bookkeepers } = useBookkeepers()
  const saveDefault = useSetDefaultSigners()

  // local edits keyed by companyId
  const [edits, setEdits] = useState<Record<number, Edit>>({})
  useEffect(() => { setEdits({}) }, [data])

  const auditorOpts = (auditors ?? []).filter((a) => a.isActive)
  const bkOpts = (bookkeepers ?? []).filter((b) => b.isActive)

  function rowValue(companyId: number, field: keyof Edit, original: number | null | undefined): number | '' {
    const e = edits[companyId]
    if (e && e[field] !== undefined) return e[field]
    return original ?? ''
  }
  function setRow(companyId: number, field: keyof Edit, value: number | '', orig: Edit) {
    setEdits((p) => ({ ...p, [companyId]: { ...orig, ...p[companyId], [field]: value } }))
  }

  const dirty = Object.keys(edits).map(Number).filter((id) => {
    const row = (data ?? []).find((r) => r.companyId === id)
    if (!row) return false
    const e = edits[id]
    return e.auditorId !== (row.defaultAuditorId ?? '') || e.bookkeeperId !== (row.defaultBookkeeperId ?? '')
  })

  async function saveAll() {
    for (const id of dirty) {
      const e = edits[id]
      await saveDefault.mutateAsync({ companyId: id, data: { auditorId: e.auditorId || null, bookkeeperId: e.bookkeeperId || null } })
    }
    setEdits({})
  }

  return (
    <div>
      <PageHeader title="มอบหมายผู้ลงนาม (ภาพรวมทุกบริษัท)" />
      <Card className="mb-4 p-4">
        <p className="mb-3 text-sm text-gray-500">
          ตั้ง "ผู้ลงนามประจำ" (ใช้ทุกปีอัตโนมัติ) ของแต่ละบริษัทในที่เดียว — ผู้สอบ/ผู้ทำบัญชี 1 คนดูแลได้หลายบริษัท.
          กรองตามผู้สอบ/ผู้ทำบัญชีเพื่อดูว่าใครดูแลบริษัทใดบ้าง. (override รายปีแก้ในหน้า ภ.ง.ด.50 ของบริษัทนั้น)
        </p>
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ค้นหาบริษัท</label>
            <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="ชื่อ/รหัส"
              className="w-56 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">กรองผู้สอบบัญชี</label>
            <select value={filterAuditor} onChange={(e) => setFilterAuditor(e.target.value ? Number(e.target.value) : '')}
              className="w-52 rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">— ทั้งหมด —</option>
              {(auditors ?? []).map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">กรองผู้ทำบัญชี</label>
            <select value={filterBk} onChange={(e) => setFilterBk(e.target.value ? Number(e.target.value) : '')}
              className="w-52 rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">— ทั้งหมด —</option>
              {(bookkeepers ?? []).map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
            </select>
          </div>
          {dirty.length > 0 && (
            <Button type="button" onClick={saveAll} disabled={saveDefault.isPending} className="ml-auto">
              {saveDefault.isPending ? 'กำลังบันทึก...' : `บันทึกการเปลี่ยนแปลง (${dirty.length})`}
            </Button>
          )}
        </div>
      </Card>

      {isLoading ? <StateMessage>กำลังโหลด...</StateMessage>
        : isError ? <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>
        : (
        <Card className="overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-500">
              <tr>
                <th className="px-3 py-2 text-left">บริษัท</th>
                <th className="px-3 py-2 text-left">ผู้สอบบัญชีประจำ</th>
                <th className="px-3 py-2 text-left">ผู้ทำบัญชีประจำ</th>
                <th className="px-3 py-2 text-center">override รายปี</th>
              </tr>
            </thead>
            <tbody>
              {(data ?? []).map((r) => {
                const orig: Edit = { auditorId: r.defaultAuditorId ?? '', bookkeeperId: r.defaultBookkeeperId ?? '' }
                const isDirty = dirty.includes(r.companyId)
                return (
                  <tr key={r.companyId} className={`border-t border-gray-50 ${isDirty ? 'bg-amber-50' : ''}`}>
                    <td className="px-3 py-2">
                      <div className="font-medium text-slate-700">{r.companyName}</div>
                      <div className="text-xs text-gray-400">{r.companyCode}</div>
                    </td>
                    <td className="px-3 py-2">
                      <Sel value={rowValue(r.companyId, 'auditorId', r.defaultAuditorId)}
                        onChange={(v) => setRow(r.companyId, 'auditorId', v, orig)}
                        options={auditorOpts.map((a) => ({ id: a.id, label: a.type === 2 ? `${a.name} (TA)` : a.name }))}
                        current={r.defaultAuditorId} currentLabel={r.defaultAuditorName} />
                    </td>
                    <td className="px-3 py-2">
                      <Sel value={rowValue(r.companyId, 'bookkeeperId', r.defaultBookkeeperId)}
                        onChange={(v) => setRow(r.companyId, 'bookkeeperId', v, orig)}
                        options={bkOpts.map((b) => ({ id: b.id, label: b.name }))}
                        current={r.defaultBookkeeperId} currentLabel={r.defaultBookkeeperName} />
                    </td>
                    <td className="px-3 py-2 text-center text-gray-500">{r.overrideYears > 0 ? `${r.overrideYears} ปี` : '—'}</td>
                  </tr>
                )
              })}
              {(data ?? []).length === 0 && <tr><td colSpan={4} className="px-3 py-6 text-center text-gray-400">ไม่พบบริษัท</td></tr>}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}

// dropdown ที่รวมตัวเลือกปัจจุบัน (เผื่อ master ถูกปิดใช้งานแต่ยังถูกเลือกอยู่)
function Sel({
  value, onChange, options, current, currentLabel,
}: {
  value: number | ''
  onChange: (v: number | '') => void
  options: { id: number; label: string }[]
  current?: number | null
  currentLabel?: string | null
}) {
  const hasCurrent = current && !options.some((o) => o.id === current)
  return (
    <select value={value} onChange={(e) => onChange(e.target.value ? Number(e.target.value) : '')}
      className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400">
      <option value="">— ไม่ระบุ —</option>
      {hasCurrent && <option value={current!}>{currentLabel} (ปิดใช้งาน)</option>}
      {options.map((o) => <option key={o.id} value={o.id}>{o.label}</option>)}
    </select>
  )
}
