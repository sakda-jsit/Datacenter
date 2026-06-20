import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import { useAssignableUsers, useCreateTask, useUpdateTask } from '../hooks/useTasks'
import { PRIORITY_OPTIONS, RECURRENCE_OPTIONS } from '../types/task.types'
import type { WorkTaskDto } from '../types/task.types'

interface ChecklistRow {
  text: string
  isDone: boolean
}

interface Props {
  companyId: number
  editing: WorkTaskDto | null
  onClose: () => void
}

export default function TaskFormModal({ companyId, editing, onClose }: Props) {
  const { data: users } = useAssignableUsers(companyId)
  const create = useCreateTask()
  const update = useUpdateTask()

  const [title, setTitle] = useState(editing?.title ?? '')
  const [description, setDescription] = useState(editing?.description ?? '')
  const [category, setCategory] = useState(editing?.category ?? '')
  const [priority, setPriority] = useState<number>(editing?.priority ?? 1)
  const [dueDate, setDueDate] = useState(editing?.dueDate ? editing.dueDate.slice(0, 10) : '')
  const [assignedUserId, setAssignedUserId] = useState<number | ''>(editing?.assignedUserId ?? '')
  const [recurrenceType, setRecurrenceType] = useState<number>(editing?.recurrenceType ?? 0)
  const [recurrenceInterval, setRecurrenceInterval] = useState<number>(editing?.recurrenceInterval ?? 1)
  const [items, setItems] = useState<ChecklistRow[]>(
    editing?.items?.map((i) => ({ text: i.text, isDone: i.isDone })) ?? [],
  )
  const [error, setError] = useState('')

  function updateItem(idx: number, patch: Partial<ChecklistRow>) {
    setItems((prev) => prev.map((it, i) => (i === idx ? { ...it, ...patch } : it)))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!title.trim()) return setError('ต้องระบุชื่องาน')

    const payload = {
      title: title.trim(),
      description: description || null,
      category: category || null,
      priority,
      dueDate: dueDate ? `${dueDate}T00:00:00` : null,
      assignedUserId: assignedUserId === '' ? null : Number(assignedUserId),
      recurrenceType,
      recurrenceInterval: recurrenceInterval < 1 ? 1 : recurrenceInterval,
      items: items.filter((it) => it.text.trim()).map((it) => ({ text: it.text.trim(), isDone: it.isDone })),
    }
    try {
      if (editing) await update.mutateAsync({ id: editing.id, ...payload })
      else await create.mutateAsync({ clientCompanyId: companyId, ...payload })
      onClose()
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  const saving = create.isPending || update.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">{editing ? 'แก้ไขงาน' : 'สร้างงานใหม่'}</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-4">
          <div className="mb-3">
            <label className="mb-1 block text-xs font-medium text-gray-600">ชื่องาน *</label>
            <input
              type="text" value={title} onChange={(e) => setTitle(e.target.value)} autoFocus
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
          </div>
          <div className="mb-3">
            <label className="mb-1 block text-xs font-medium text-gray-600">รายละเอียด</label>
            <textarea
              value={description ?? ''} onChange={(e) => setDescription(e.target.value)} rows={2}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
          </div>
          <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">หมวด</label>
              <input
                type="text" value={category ?? ''} onChange={(e) => setCategory(e.target.value)} placeholder="เช่น เอกสาร, งบ, ภาษี"
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ความสำคัญ</label>
              <select
                value={priority} onChange={(e) => setPriority(Number(e.target.value))}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              >
                {PRIORITY_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">กำหนดส่ง</label>
              <input
                type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ผู้รับผิดชอบ</label>
              <select
                value={assignedUserId} onChange={(e) => setAssignedUserId(e.target.value === '' ? '' : Number(e.target.value))}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              >
                <option value="">— ไม่ระบุ —</option>
                {(users ?? []).map((u) => <option key={u.userId} value={u.userId}>{u.displayName}</option>)}
              </select>
            </div>
          </div>

          {/* งานประจำ (recurring) */}
          <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ทำซ้ำ (งานประจำ)</label>
              <select
                value={recurrenceType} onChange={(e) => setRecurrenceType(Number(e.target.value))}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              >
                {RECURRENCE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            {recurrenceType !== 0 && (
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">ทุก ๆ (จำนวนรอบ)</label>
                <input
                  type="number" min={1} value={recurrenceInterval}
                  onChange={(e) => setRecurrenceInterval(Number(e.target.value))}
                  className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
              </div>
            )}
          </div>
          {recurrenceType !== 0 && (
            <p className="mb-3 -mt-1 text-xs text-slate-400">เมื่อปิดงานนี้ ระบบจะสร้างงานถัดไปอัตโนมัติ (เลื่อนกำหนดส่งตามรอบ)</p>
          )}

          {/* checklist */}
          <div className="mb-3">
            <div className="mb-1 flex items-center justify-between">
              <label className="block text-xs font-medium text-gray-600">รายการย่อย (checklist)</label>
              <button type="button" onClick={() => setItems((p) => [...p, { text: '', isDone: false }])}
                className="text-xs font-bold text-sky-600 hover:underline">+ เพิ่มรายการ</button>
            </div>
            {items.length === 0 && <p className="text-xs text-slate-400">— ไม่มีรายการย่อย —</p>}
            <div className="space-y-1.5">
              {items.map((it, idx) => (
                <div key={idx} className="flex items-center gap-2">
                  <input type="checkbox" checked={it.isDone} onChange={(e) => updateItem(idx, { isDone: e.target.checked })} className="rounded" />
                  <input
                    type="text" value={it.text} placeholder={`ขั้นตอนที่ ${idx + 1}`}
                    onChange={(e) => updateItem(idx, { text: e.target.value })}
                    className="flex-1 rounded border border-gray-300 px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-slate-400"
                  />
                  <button type="button" onClick={() => setItems((p) => p.filter((_, i) => i !== idx))}
                    className="text-slate-400 hover:text-red-500">×</button>
                </div>
              ))}
            </div>
          </div>

          {error && <p className="mb-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>ยกเลิก</Button>
            <Button type="submit" disabled={saving}>{saving ? 'กำลังบันทึก...' : 'บันทึก'}</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
