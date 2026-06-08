import { useEffect, useRef, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import {
  useCreateEnrollment,
  useDeleteDocument,
  useEmployee,
  useSaveEmployee,
  useUpdateEnrollment,
  useUploadDocument,
} from '../hooks/usePayroll'
import { payrollApi } from '../services/payrollApi'
import {
  DOC_TYPE_LABEL,
  ENROLL_STATUS_LABEL,
  ENROLL_TYPE_LABEL,
  EmployeeDocType,
  SsoEnrollmentType,
  type EmployeeInput,
} from '../types/payroll.types'

interface Props {
  companyId: number
  employeeId: number | null
  onClose: () => void
}

const EMPTY: EmployeeInput = {
  employeeCode: '', nationalId: '', prefix: '', firstName: '', lastName: '',
  birthDate: null, maritalStatus: '', nationality: 'ไทย', address: '', addressDetail: {}, position: '', department: '',
  startDate: new Date().toISOString().slice(0, 10), resignDate: null,
  employmentStatus: 1, salaryType: 1, baseSalary: 0, dailyWage: null,
  ssoNumber: '', ssoHospital: '', ssoStatus: 0, taxId: '', note: '',
}

// ช่องที่อยู่แยกตามแบบสรรพากร (e-Filing ภ.ง.ด.1ก) — key ใน EmployeeAddress + ป้าย
const ADDR_FIELDS: { k: keyof NonNullable<EmployeeInput['addressDetail']>; label: string }[] = [
  { k: 'houseNo', label: 'เลขที่' }, { k: 'moo', label: 'หมู่ที่' }, { k: 'village', label: 'หมู่บ้าน' },
  { k: 'soi', label: 'ซอย/ตรอก' }, { k: 'yaek', label: 'แยก' }, { k: 'road', label: 'ถนน' },
  { k: 'building', label: 'อาคาร' }, { k: 'roomNo', label: 'เลขที่ห้อง' }, { k: 'floor', label: 'ชั้น' },
  { k: 'subDistrict', label: 'ตำบล/แขวง' }, { k: 'district', label: 'อำเภอ/เขต' },
  { k: 'province', label: 'จังหวัด' }, { k: 'postalCode', label: 'รหัสไปรษณีย์' },
]

function d(s?: string | null) {
  return s ? s.slice(0, 10) : ''
}

export default function EmployeeFormModal({ companyId, employeeId, onClose }: Props) {
  const [currentId, setCurrentId] = useState<number | null>(employeeId)
  const { data: detail } = useEmployee(companyId, currentId)
  const save = useSaveEmployee(companyId)
  const [form, setForm] = useState<EmployeeInput>(EMPTY)
  const [error, setError] = useState('')

  useEffect(() => {
    if (detail) {
      setForm({
        employeeCode: detail.employeeCode, nationalId: detail.nationalId, prefix: detail.prefix ?? '',
        firstName: detail.firstName, lastName: detail.lastName, birthDate: d(detail.birthDate) || null,
        maritalStatus: detail.maritalStatus ?? '', nationality: detail.nationality ?? '',
        address: detail.address ?? '', addressDetail: detail.addressDetail ?? {},
        position: detail.position ?? '', department: detail.department ?? '',
        startDate: d(detail.startDate), resignDate: d(detail.resignDate) || null,
        employmentStatus: detail.employmentStatus, salaryType: detail.salaryType,
        baseSalary: detail.baseSalary, dailyWage: detail.dailyWage ?? null,
        ssoNumber: detail.ssoNumber ?? '', ssoHospital: detail.ssoHospital ?? '',
        ssoStatus: detail.ssoStatus, taxId: detail.taxId ?? '', note: detail.note ?? '',
      })
    }
  }, [detail])

  function set<K extends keyof EmployeeInput>(k: K, v: EmployeeInput[K]) {
    setForm((p) => ({ ...p, [k]: v }))
  }
  function setAddr(k: keyof NonNullable<EmployeeInput['addressDetail']>, v: string) {
    setForm((p) => ({ ...p, addressDetail: { ...(p.addressDetail ?? {}), [k]: v } }))
  }

  async function handleSave() {
    setError('')
    try {
      const res = await save.mutateAsync({ id: currentId, data: form })
      if (!currentId && res && typeof (res as { id?: number }).id === 'number') {
        setCurrentId((res as { id: number }).id) // เปิดส่วนเอกสาร/ปกส. ต่อ
      }
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-3xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">{currentId ? 'แก้ไขพนักงาน' : 'เพิ่มพนักงาน'}</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="max-h-[70vh] space-y-5 overflow-y-auto px-6 py-4">
          {/* ── ข้อมูลพนักงาน ── */}
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <Field label="รหัสพนักงาน *"><input className={inp} value={form.employeeCode} onChange={(e) => set('employeeCode', e.target.value)} /></Field>
            <Field label="เลขบัตรประชาชน"><input className={inp} value={form.nationalId} onChange={(e) => set('nationalId', e.target.value)} /></Field>
            <Field label="คำนำหน้า"><input className={inp} value={form.prefix} onChange={(e) => set('prefix', e.target.value)} /></Field>
            <Field label="ชื่อ *"><input className={inp} value={form.firstName} onChange={(e) => set('firstName', e.target.value)} /></Field>
            <Field label="นามสกุล"><input className={inp} value={form.lastName} onChange={(e) => set('lastName', e.target.value)} /></Field>
            <Field label="วันเกิด"><input type="date" className={inp} value={d(form.birthDate)} onChange={(e) => set('birthDate', e.target.value || null)} /></Field>
            <Field label="สถานภาพ"><input className={inp} value={form.maritalStatus} onChange={(e) => set('maritalStatus', e.target.value)} placeholder="โสด/สมรส" /></Field>
            <Field label="ตำแหน่ง"><input className={inp} value={form.position} onChange={(e) => set('position', e.target.value)} /></Field>
            <Field label="ฝ่าย"><input className={inp} value={form.department} onChange={(e) => set('department', e.target.value)} /></Field>
            <div className="col-span-2 sm:col-span-3"><Field label="ที่อยู่"><input className={inp} value={form.address} onChange={(e) => set('address', e.target.value)} /></Field></div>
            <Field label="วันเริ่มงาน *"><input type="date" className={inp} value={d(form.startDate)} onChange={(e) => set('startDate', e.target.value)} /></Field>
            <Field label="วันลาออก"><input type="date" className={inp} value={d(form.resignDate)} onChange={(e) => set('resignDate', e.target.value || null)} /></Field>
            <Field label="สถานะ">
              <select className={inp} value={form.employmentStatus} onChange={(e) => set('employmentStatus', Number(e.target.value))}>
                <option value={1}>ปกติ</option><option value={2}>ลาออก</option>
              </select>
            </Field>
            <Field label="ประเภทค่าจ้าง">
              <select className={inp} value={form.salaryType} onChange={(e) => set('salaryType', Number(e.target.value))}>
                <option value={1}>รายเดือน</option><option value={2}>รายวัน</option>
              </select>
            </Field>
            <Field label="เงินเดือน/ฐาน"><input type="number" className={inp} value={form.baseSalary} onChange={(e) => set('baseSalary', Number(e.target.value))} /></Field>
            <Field label="ค่าจ้างรายวัน"><input type="number" className={inp} value={form.dailyWage ?? ''} onChange={(e) => set('dailyWage', e.target.value === '' ? null : Number(e.target.value))} /></Field>
            <Field label="เลขผู้ประกันตน"><input className={inp} value={form.ssoNumber} onChange={(e) => set('ssoNumber', e.target.value)} /></Field>
            <Field label="โรงพยาบาล ปกส."><input className={inp} value={form.ssoHospital} onChange={(e) => set('ssoHospital', e.target.value)} /></Field>
            <Field label="เลขผู้เสียภาษี"><input className={inp} value={form.taxId} onChange={(e) => set('taxId', e.target.value)} /></Field>
          </div>

          {/* ── ที่อยู่แยกช่อง (สำหรับ e-Filing ภ.ง.ด.1ก) ── */}
          <details className="rounded-lg border border-gray-200">
            <summary className="cursor-pointer px-3 py-2 text-sm font-semibold text-slate-800">
              ที่อยู่แยกช่อง (สำหรับไฟล์ e-Filing ภ.ง.ด.1ก) <span className="font-normal text-gray-400">— optional</span>
            </summary>
            <div className="grid grid-cols-2 gap-3 px-3 pb-3 sm:grid-cols-3">
              {ADDR_FIELDS.map((f) => (
                <Field key={f.k} label={f.label}>
                  <input className={inp} value={form.addressDetail?.[f.k] ?? ''} onChange={(e) => setAddr(f.k, e.target.value)} />
                </Field>
              ))}
            </div>
          </details>

          {error && <p className="rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

          {/* ── เอกสาร + ปกส. (เฉพาะเมื่อบันทึกแล้ว) ── */}
          {currentId ? (
            <>
              <DocumentsSection companyId={companyId} employeeId={currentId} docs={detail?.documents ?? []} />
              <EnrollmentSection companyId={companyId} employeeId={currentId} enrollments={detail?.ssoEnrollments ?? []} docs={detail?.documents ?? []} />
            </>
          ) : (
            <p className="rounded bg-slate-50 px-3 py-2 text-xs text-gray-500">บันทึกพนักงานก่อน จึงจะแนบรูปบัตร/หลักฐาน และแจ้งเข้า-ออก ปกส. ได้</p>
          )}
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
          <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
          <Button type="button" onClick={handleSave} disabled={save.isPending}>
            {save.isPending ? 'กำลังบันทึก...' : currentId ? 'บันทึกการแก้ไข' : 'บันทึก'}
          </Button>
        </div>
      </div>
    </div>
  )
}

const inp = 'w-full rounded border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'
function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-gray-600">{label}</span>
      {children}
    </label>
  )
}

// ── เอกสาร/หลักฐาน ───────────────────────────────────────────────────────────
function DocumentsSection({ companyId, employeeId, docs }: { companyId: number; employeeId: number; docs: { id: number; docType: number; fileName: string; note?: string | null }[] }) {
  const upload = useUploadDocument(companyId, employeeId)
  const del = useDeleteDocument(companyId, employeeId)
  const fileRef = useRef<HTMLInputElement>(null)
  const [docType, setDocType] = useState(EmployeeDocType.IdCardFront as number)
  const [busy, setBusy] = useState(false)

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    setBusy(true)
    try { await upload.mutateAsync({ docType, file }) } finally { setBusy(false) }
  }
  async function download(id: number) {
    const blob = await payrollApi.downloadDocument(id, companyId)
    const url = URL.createObjectURL(blob)
    window.open(url, '_blank')
    setTimeout(() => URL.revokeObjectURL(url), 60000)
  }

  return (
    <section className="rounded-lg border border-gray-200 p-3">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-800">เอกสาร / หลักฐาน (PDPA)</p>
        <div className="flex items-center gap-2">
          <select className="rounded border border-gray-300 px-2 py-1 text-xs" value={docType} onChange={(e) => setDocType(Number(e.target.value))}>
            {Object.entries(DOC_TYPE_LABEL).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
          <input ref={fileRef} type="file" onChange={onFile} className="hidden" />
          <Button type="button" variant="secondary" onClick={() => fileRef.current?.click()} disabled={busy}>
            {busy ? 'กำลังอัปโหลด...' : 'อัปโหลด'}
          </Button>
        </div>
      </div>
      {docs.length === 0 ? (
        <p className="text-xs text-gray-400">ยังไม่มีเอกสาร</p>
      ) : (
        <ul className="space-y-1 text-xs">
          {docs.map((doc) => (
            <li key={doc.id} className="flex items-center justify-between rounded bg-slate-50 px-2 py-1">
              <span><span className="font-medium text-slate-700">{DOC_TYPE_LABEL[doc.docType]}</span> · {doc.fileName}</span>
              <span className="flex gap-2">
                <button type="button" className="text-sky-600 hover:underline" onClick={() => download(doc.id)}>เปิด</button>
                <button type="button" className="text-red-500 hover:underline" onClick={() => del.mutate(doc.id)}>ลบ</button>
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

// ── แจ้งเข้า-ออก ปกส. ────────────────────────────────────────────────────────
function EnrollmentSection({ companyId, employeeId, enrollments, docs }: {
  companyId: number; employeeId: number
  enrollments: { id: number; type: number; eventDate: string; submittedDate?: string | null; status: number; proofDocumentId?: number | null }[]
  docs: { id: number; fileName: string }[]
}) {
  const create = useCreateEnrollment(companyId, employeeId)
  const update = useUpdateEnrollment(companyId, employeeId)
  const [type, setType] = useState(SsoEnrollmentType.Enroll as number)
  const [eventDate, setEventDate] = useState(new Date().toISOString().slice(0, 10))

  return (
    <section className="rounded-lg border border-gray-200 p-3">
      <p className="mb-2 text-sm font-semibold text-slate-800">การแจ้งเข้า-ออก ประกันสังคม</p>
      <div className="mb-3 flex flex-wrap items-end gap-2">
        <select className="rounded border border-gray-300 px-2 py-1 text-xs" value={type} onChange={(e) => setType(Number(e.target.value))}>
          <option value={1}>แจ้งเข้า</option><option value={2}>แจ้งออก</option>
        </select>
        <input type="date" className="rounded border border-gray-300 px-2 py-1 text-xs" value={eventDate} onChange={(e) => setEventDate(e.target.value)} />
        <Button type="button" variant="secondary" onClick={() => create.mutate({ employeeId, type, eventDate })} disabled={create.isPending}>+ เพิ่มการแจ้ง</Button>
      </div>
      {enrollments.length === 0 ? (
        <p className="text-xs text-gray-400">ยังไม่มีรายการ</p>
      ) : (
        <ul className="space-y-1 text-xs">
          {enrollments.map((en) => (
            <li key={en.id} className="flex items-center justify-between rounded bg-slate-50 px-2 py-1">
              <span>
                <span className="font-medium text-slate-700">{ENROLL_TYPE_LABEL[en.type]}</span> · {en.eventDate.slice(0, 10)} ·{' '}
                <span className={en.status === 1 ? 'text-green-700' : 'text-amber-600'}>{ENROLL_STATUS_LABEL[en.status]}</span>
                {en.submittedDate ? ` (แจ้ง ${en.submittedDate.slice(0, 10)})` : ''}
              </span>
              {en.status === 0 && (
                <span className="flex items-center gap-1">
                  <select className="rounded border border-gray-300 px-1 py-0.5 text-[11px]" defaultValue="" id={`proof-${en.id}`}>
                    <option value="">แนบหลักฐาน…</option>
                    {docs.map((dc) => <option key={dc.id} value={dc.id}>{dc.fileName}</option>)}
                  </select>
                  <button
                    type="button" className="text-sky-600 hover:underline"
                    onClick={() => {
                      const sel = document.getElementById(`proof-${en.id}`) as HTMLSelectElement | null
                      const proofDocumentId = sel && sel.value ? Number(sel.value) : null
                      update.mutate({ id: en.id, body: { submittedDate: new Date().toISOString().slice(0, 10), status: 1, proofDocumentId } })
                    }}
                  >ทำเครื่องหมายแจ้งแล้ว</button>
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
