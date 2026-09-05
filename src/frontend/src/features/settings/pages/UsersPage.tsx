import { useMemo, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAuth } from '../../../shared/hooks/useAuth'
import { useClientList } from '../../clients/hooks/useClients'
import {
  useCreateUser,
  useResetUserPassword,
  useUnlockUser,
  useUpdateUser,
  useUsers,
} from '../hooks/useUsers'
import {
  USER_ROLE,
  USER_ROLE_DESC,
  USER_ROLE_LABEL,
  type SystemUser,
} from '../services/usersApi'

interface FormState {
  username: string
  displayName: string
  email: string
  role: number
  password: string
  isActive: boolean
  companyIds: number[]
}

const blank: FormState = {
  username: '',
  displayName: '',
  email: '',
  role: USER_ROLE.Maker,
  password: '',
  isActive: true,
  companyIds: [],
}

function passwordProblem(password: string) {
  if (password.length < 8) return 'รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร'
  if (!/[a-zA-Z]/.test(password) || !/\d/.test(password)) return 'รหัสผ่านต้องมีทั้งตัวอักษรและตัวเลข'
  return ''
}

function fmtDate(v?: string | null) {
  if (!v) return '–'
  return new Date(v).toLocaleString('th-TH', { dateStyle: 'short', timeStyle: 'short' })
}

export default function UsersPage() {
  const { user: me, isAdmin } = useAuth()
  const { data: users, isLoading, isError } = useUsers()
  const { data: clients } = useClientList({ pageNumber: 1, pageSize: 500 })
  const create = useCreateUser()
  const update = useUpdateUser()
  const reset = useResetUserPassword()
  const unlock = useUnlockUser()

  const [editId, setEditId] = useState<number | null>(null) // 0 = สร้างใหม่
  const [form, setForm] = useState<FormState>(blank)
  const [resetFor, setResetFor] = useState<SystemUser | null>(null)
  const [resetPassword, setResetPassword] = useState('')
  const [error, setError] = useState('')
  const [companySearch, setCompanySearch] = useState('')

  const companies = useMemo(
    () => (clients?.items ?? []).filter((c) => c.isActive).sort((a, b) => a.name.localeCompare(b.name, 'th')),
    [clients],
  )

  // ค้นหาชื่อบริษัทในรายการติ๊ก — ตัดช่องว่างซ้ำเพื่อให้พิมพ์เว้นวรรคเกินก็ยังเจอ
  const companyQuery = companySearch.trim().replace(/\s+/g, ' ').toLowerCase()
  const shownCompanies = useMemo(
    () => (companyQuery ? companies.filter((c) => c.name.toLowerCase().includes(companyQuery)) : companies),
    [companies, companyQuery],
  )
  /** บริษัทที่ติ๊กไว้แต่ถูกคำค้นซ่อนอยู่ — เตือนไม่ให้เข้าใจผิดว่าติ๊กหาย */
  const hiddenSelectedCount = useMemo(() => {
    if (!companyQuery) return 0
    const shown = new Set(shownCompanies.map((c) => c.id))
    return form.companyIds.filter((id) => !shown.has(id)).length
  }, [companyQuery, shownCompanies, form.companyIds])

  function set<K extends keyof FormState>(k: K, v: FormState[K]) {
    setForm((p) => ({ ...p, [k]: v }))
  }

  function startNew() {
    setError('')
    setCompanySearch('')
    setEditId(0)
    setForm(blank)
  }

  function startEdit(u: SystemUser) {
    setError('')
    setCompanySearch('')
    setEditId(u.id)
    setForm({
      username: u.username,
      displayName: u.displayName,
      email: u.email ?? '',
      role: u.role,
      password: '',
      isActive: u.isActive,
      companyIds: [...u.companyIds],
    })
  }

  /** หัวหน้างานแตะบัญชีผู้ดูแลระบบไม่ได้ (API ปฏิเสธอยู่แล้ว — ซ่อนปุ่มไม่ให้กดเสียเที่ยว) */
  function canManageUser(u: SystemUser) {
    return isAdmin || u.role !== USER_ROLE.Admin
  }

  function toggleCompany(id: number) {
    setForm((p) => ({
      ...p,
      companyIds: p.companyIds.includes(id) ? p.companyIds.filter((x) => x !== id) : [...p.companyIds, id],
    }))
  }

  const creating = editId === 0
  const formInvalid =
    !form.displayName.trim() ||
    (creating && (!form.username.trim() || !!passwordProblem(form.password)))

  function apiError(err: unknown) {
    const res = (err as { response?: { data?: { title?: string; errors?: Record<string, string[]> } } }).response
    const field = res?.data?.errors ? Object.values(res.data.errors).flat()[0] : undefined
    return field || res?.data?.title || 'บันทึกไม่สำเร็จ'
  }

  async function onSave() {
    if (formInvalid) return
    setError('')
    try {
      if (creating) {
        await create.mutateAsync({
          username: form.username.trim(),
          displayName: form.displayName.trim(),
          email: form.email.trim() || null,
          role: form.role,
          password: form.password,
          companyIds: form.role === USER_ROLE.Admin ? [] : form.companyIds,
        })
      } else {
        await update.mutateAsync({
          id: editId!,
          data: {
            displayName: form.displayName.trim(),
            email: form.email.trim() || null,
            role: form.role,
            isActive: form.isActive,
            companyIds: form.role === USER_ROLE.Admin ? [] : form.companyIds,
          },
        })
      }
      setEditId(null)
    } catch (err) {
      setError(apiError(err))
    }
  }

  async function onReset() {
    if (!resetFor || passwordProblem(resetPassword)) return
    setError('')
    try {
      await reset.mutateAsync({ id: resetFor.id, newPassword: resetPassword })
      setResetFor(null)
      setResetPassword('')
    } catch (err) {
      setError(apiError(err))
    }
  }

  return (
    <div className="max-w-5xl">
      <PageHeader title="ผู้ใช้งานระบบ" />

      <Card className="mb-4 p-4">
        <p className="text-sm text-gray-500">
          สร้างบัญชีแยกให้พนักงานแต่ละคน — ห้ามใช้บัญชีร่วมกัน เพราะประวัติการใช้งาน (audit log)
          และผู้บันทึกรายการต้องระบุตัวบุคคลได้
        </p>
        <p className="mt-1 text-xs text-gray-500">
          ผู้ดูแลระบบ (Admin) เห็นข้อมูลทุกบริษัทโดยไม่ต้องผูกสิทธิ์ · Maker/Checker เห็นเฉพาะบริษัทที่เลือกไว้
        </p>
        <p className="mt-1 text-xs text-amber-600">
          ผู้ใช้ใหม่และผู้ที่ถูกรีเซ็ตรหัส จะถูกบังคับให้เปลี่ยนรหัสผ่านเองตอนเข้าใช้งานครั้งถัดไป
        </p>
      </Card>

      {error && (
        <p className="mb-4 rounded-xl border border-red-100 bg-red-50 px-3 py-2 text-sm font-medium text-red-600">
          {error}
        </p>
      )}

      {isLoading ? (
        <StateMessage>กำลังโหลด...</StateMessage>
      ) : isError ? (
        <StateMessage tone="error">โหลดรายชื่อผู้ใช้ไม่สำเร็จ</StateMessage>
      ) : (
        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b border-gray-100 p-3">
            <span className="text-sm font-medium text-slate-700">ผู้ใช้ทั้งหมด {users?.length ?? 0} คน</span>
            <Button type="button" onClick={startNew} className="px-3 py-1 text-xs">
              + เพิ่มผู้ใช้
            </Button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-xs text-gray-500">
                <tr>
                  <th className="px-3 py-2 text-left">ชื่อผู้ใช้</th>
                  <th className="px-3 py-2 text-left">ชื่อที่แสดง</th>
                  <th className="px-3 py-2 text-left">บทบาท</th>
                  <th className="px-3 py-2 text-left">บริษัทที่ดูแล</th>
                  <th className="px-3 py-2 text-left">เข้าใช้งานล่าสุด</th>
                  <th className="px-3 py-2 text-left">สถานะ</th>
                  <th className="px-3 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {(users ?? []).map((u) => (
                  <tr key={u.id} className={`border-t border-gray-50 ${u.isActive ? '' : 'text-gray-400'}`}>
                    <td className="px-3 py-2 font-mono">{u.username}</td>
                    <td className="px-3 py-2">{u.displayName}</td>
                    <td className="px-3 py-2">{USER_ROLE_LABEL[u.role]}</td>
                    <td className="px-3 py-2">
                      {u.role === USER_ROLE.Admin ? 'ทุกบริษัท' : `${u.companyIds.length} บริษัท`}
                    </td>
                    <td className="px-3 py-2 whitespace-nowrap">{fmtDate(u.lastLoginAt)}</td>
                    <td className="px-3 py-2">
                      {!u.isActive && <span className="text-gray-400">ปิดใช้งาน</span>}
                      {u.isActive && u.isLocked && <span className="text-red-500">ถูกล็อก</span>}
                      {u.isActive && !u.isLocked && u.mustChangePassword && (
                        <span className="text-amber-600">ต้องเปลี่ยนรหัส</span>
                      )}
                      {u.isActive && !u.isLocked && !u.mustChangePassword && (
                        <span className="text-emerald-600">ใช้งาน</span>
                      )}
                      {u.id === me?.userId && <span className="ml-1 text-xs text-gray-400">(คุณ)</span>}
                    </td>
                    <td className="px-3 py-2 text-right whitespace-nowrap">
                      {canManageUser(u) ? (
                        <>
                          <button onClick={() => startEdit(u)} className="text-blue-600 hover:underline">
                            แก้ไข
                          </button>
                          <button
                            onClick={() => {
                              setResetFor(u)
                              setResetPassword('')
                            }}
                            className="ml-3 text-blue-600 hover:underline"
                          >
                            รีเซ็ตรหัส
                          </button>
                          {u.isLocked && (
                            <button onClick={() => unlock.mutate(u.id)} className="ml-3 text-amber-600 hover:underline">
                              ปลดล็อก
                            </button>
                          )}
                        </>
                      ) : (
                        <span className="text-xs text-gray-400">เฉพาะผู้ดูแลระบบแก้ได้</span>
                      )}
                    </td>
                  </tr>
                ))}
                {(users ?? []).length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-3 py-6 text-center text-gray-400">
                      ยังไม่มีผู้ใช้
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {editId !== null && (
        <Card className="mt-4 p-5">
          <h3 className="mb-3 text-sm font-semibold text-slate-800">{creating ? 'เพิ่มผู้ใช้' : 'แก้ไขผู้ใช้'}</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="ชื่อผู้ใช้ (สำหรับเข้าสู่ระบบ) *">
              <input
                value={form.username}
                onChange={(e) => set('username', e.target.value)}
                disabled={!creating}
                className={cls(creating && !form.username.trim())}
                placeholder="เช่น somchai.j"
              />
              {!creating && <p className="mt-1 text-xs text-gray-400">ชื่อผู้ใช้แก้ไม่ได้ (ใช้อ้างอิงใน audit log)</p>}
            </Field>
            <Field label="ชื่อที่แสดง *">
              <input
                value={form.displayName}
                onChange={(e) => set('displayName', e.target.value)}
                className={cls(!form.displayName.trim())}
              />
            </Field>
            <Field label="อีเมล (ใช้แจ้งเตือนงานที่มอบหมาย)">
              <input value={form.email} onChange={(e) => set('email', e.target.value)} className={cls(false)} />
            </Field>
            <Field label="บทบาท">
              <select value={form.role} onChange={(e) => set('role', Number(e.target.value))} className={cls(false)}>
                {Object.entries(USER_ROLE_LABEL)
                  // หัวหน้างานตั้งใครเป็น Admin ไม่ได้ (API ปฏิเสธอยู่แล้ว — ซ่อนไว้ไม่ให้กดเสียเที่ยว)
                  .filter(([k]) => isAdmin || Number(k) !== USER_ROLE.Admin)
                  .map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
              </select>
              <p className="mt-1 text-xs text-gray-400">{USER_ROLE_DESC[form.role]}</p>
            </Field>
            {creating && (
              <Field label="รหัสผ่านชั่วคราว *">
                <input
                  type="text"
                  value={form.password}
                  onChange={(e) => set('password', e.target.value)}
                  className={cls(!!passwordProblem(form.password))}
                  placeholder="อย่างน้อย 8 ตัว มีตัวอักษร+ตัวเลข"
                />
                {form.password.length > 0 && passwordProblem(form.password) && (
                  <p className="mt-1 text-xs text-red-500">{passwordProblem(form.password)}</p>
                )}
              </Field>
            )}
            {!creating && (
              <Field label="สถานะ">
                <label className="flex items-center gap-2 py-2 text-sm text-gray-600">
                  <input
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => set('isActive', e.target.checked)}
                  />
                  ใช้งาน (ปิดเพื่อระงับการเข้าระบบทันที)
                </label>
              </Field>
            )}
          </div>

          {form.role !== USER_ROLE.Admin && (
            <div className="mt-4">
              <p className="mb-1 text-xs font-medium text-gray-600">
                บริษัทที่ดูแล — ทำรายการได้ (เลือกแล้ว {form.companyIds.length} จาก {companies.length})
              </p>
              <p className="mb-2 text-xs text-gray-400">
                ผู้ใช้ทุกคน <b>ดูข้อมูลได้ทุกบริษัท</b>อยู่แล้ว — ที่เลือกตรงนี้คือบริษัทที่ผู้ใช้คนนี้
                <b>บันทึก/แก้ไข/นำเข้าข้อมูล</b> และ <b>ดูข้อมูลเงินเดือน</b> ได้
              </p>
              <div className="relative mb-2">
                <input
                  type="search"
                  value={companySearch}
                  onChange={(e) => setCompanySearch(e.target.value)}
                  placeholder="ค้นหาชื่อบริษัท..."
                  className="w-full rounded-lg border border-slate-300 py-1.5 pl-8 pr-3 text-sm
                             focus:border-blue-400 focus:outline-none focus:ring-1 focus:ring-blue-200"
                />
                <span className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-sm text-gray-400">
                  🔍
                </span>
              </div>
              <div className="flex flex-wrap items-center gap-3 pb-2 text-xs">
                <button
                  type="button"
                  onClick={() =>
                    set('companyIds', [
                      ...new Set([...form.companyIds, ...shownCompanies.map((c) => c.id)]),
                    ])
                  }
                  className="text-blue-600 hover:underline"
                >
                  {companyQuery ? `เลือกที่ค้นเจอ (${shownCompanies.length})` : 'เลือกทั้งหมด'}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    if (!companyQuery) return set('companyIds', [])
                    const shown = new Set(shownCompanies.map((c) => c.id))
                    set('companyIds', form.companyIds.filter((id) => !shown.has(id)))
                  }}
                  className="text-blue-600 hover:underline"
                >
                  {companyQuery ? 'ล้างที่ค้นเจอ' : 'ล้างทั้งหมด'}
                </button>
                {hiddenSelectedCount > 0 && (
                  <span className="text-gray-400">
                    (ยังมีอีก {hiddenSelectedCount} บริษัทที่เลือกไว้แต่ไม่ตรงคำค้น)
                  </span>
                )}
              </div>
              <div className="max-h-64 overflow-y-auto rounded-xl border border-slate-200 p-3">
                <div className="grid gap-1 sm:grid-cols-2">
                  {shownCompanies.map((c) => (
                    <label key={c.id} className="flex items-center gap-2 text-sm text-gray-700">
                      <input
                        type="checkbox"
                        checked={form.companyIds.includes(c.id)}
                        onChange={() => toggleCompany(c.id)}
                      />
                      <span className="truncate">{c.name}</span>
                    </label>
                  ))}
                  {companies.length === 0 && <p className="text-sm text-gray-400">ยังไม่มีบริษัทลูกค้าในระบบ</p>}
                  {companies.length > 0 && shownCompanies.length === 0 && (
                    <p className="text-sm text-gray-400">ไม่พบบริษัทที่ตรงกับ “{companySearch.trim()}”</p>
                  )}
                </div>
              </div>
            </div>
          )}

          <div className="mt-4 flex gap-2">
            <Button type="button" onClick={onSave} disabled={formInvalid || create.isPending || update.isPending}>
              {create.isPending || update.isPending ? 'กำลังบันทึก...' : 'บันทึก'}
            </Button>
            <Button type="button" variant="secondary" onClick={() => setEditId(null)}>
              ยกเลิก
            </Button>
          </div>
        </Card>
      )}

      {resetFor && (
        <Card className="mt-4 p-5">
          <h3 className="mb-1 text-sm font-semibold text-slate-800">
            รีเซ็ตรหัสผ่านของ {resetFor.displayName} ({resetFor.username})
          </h3>
          <p className="mb-3 text-xs text-gray-500">
            ตั้งรหัสชั่วคราวแล้วแจ้งเจ้าตัว — ระบบจะบังคับให้เปลี่ยนรหัสเองตอนเข้าใช้งานครั้งถัดไป
            และตัดการเข้าใช้งานที่ค้างอยู่ทั้งหมด
          </p>
          <div className="max-w-sm">
            <input
              type="text"
              value={resetPassword}
              onChange={(e) => setResetPassword(e.target.value)}
              className={cls(!!passwordProblem(resetPassword))}
              placeholder="รหัสชั่วคราว (8 ตัวขึ้นไป มีตัวอักษร+ตัวเลข)"
            />
            {resetPassword.length > 0 && passwordProblem(resetPassword) && (
              <p className="mt-1 text-xs text-red-500">{passwordProblem(resetPassword)}</p>
            )}
          </div>
          <div className="mt-4 flex gap-2">
            <Button type="button" onClick={onReset} disabled={!!passwordProblem(resetPassword) || reset.isPending}>
              {reset.isPending ? 'กำลังบันทึก...' : 'รีเซ็ตรหัสผ่าน'}
            </Button>
            <Button type="button" variant="secondary" onClick={() => setResetFor(null)}>
              ยกเลิก
            </Button>
          </div>
        </Card>
      )}
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-gray-600">{label}</label>
      {children}
    </div>
  )
}

function cls(invalid: boolean) {
  return `w-full rounded-xl border px-3 py-2 text-sm focus:outline-none focus:ring-4 focus:ring-sky-100 disabled:bg-slate-50 disabled:text-gray-500 ${
    invalid ? 'border-red-300' : 'border-slate-200 focus:border-sky-400'
  }`
}
