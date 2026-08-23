import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../../shared/services/apiClient'
import { readAuthUser } from '../../shared/hooks/useAuth'

/**
 * เปลี่ยนรหัสผ่านของตัวเอง — ใช้ทั้ง 2 กรณี:
 * (1) ถูกบังคับเปลี่ยน (ผู้ใช้ใหม่ / ผู้ดูแลรีเซ็ตรหัสให้ / รหัสตั้งต้นของระบบ) → เข้าหน้าอื่นไม่ได้จนเปลี่ยนเสร็จ
 * (2) เปลี่ยนเองตามปกติ จากเมนู "ระบบ"
 */
export default function ChangePasswordPage() {
  const navigate = useNavigate()
  const user = readAuthUser()
  const forced = !!user?.mustChangePassword

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const policyProblem =
    newPassword.length > 0 && newPassword.length < 8
      ? 'รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร'
      : newPassword.length >= 8 && !(/[a-zA-Z]/.test(newPassword) && /\d/.test(newPassword))
        ? 'รหัสผ่านต้องมีทั้งตัวอักษรและตัวเลข'
        : confirmPassword.length > 0 && newPassword !== confirmPassword
          ? 'รหัสผ่านใหม่และการยืนยันไม่ตรงกัน'
          : ''

  const invalid =
    !currentPassword || !newPassword || newPassword !== confirmPassword || !!policyProblem || saving

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (invalid) return
    setError('')
    setSaving(true)
    try {
      const { data } = await apiClient.post('/auth/change-password', { currentPassword, newPassword })
      // server ยกเลิก refresh token ใบเดิมทั้งหมดแล้ว — เก็บชุดใหม่เพื่อใช้งานต่อโดยไม่ต้อง login ซ้ำ
      localStorage.setItem('token', data.token)
      localStorage.setItem('refreshToken', data.refreshToken)
      localStorage.setItem('user', JSON.stringify(data))
      navigate('/dashboard', { replace: true })
    } catch (err) {
      const res = (err as { response?: { data?: { title?: string; errors?: Record<string, string[]> } } }).response
      const fieldError = res?.data?.errors ? Object.values(res.data.errors).flat()[0] : undefined
      setError(fieldError || res?.data?.title || 'เปลี่ยนรหัสผ่านไม่สำเร็จ')
    } finally {
      setSaving(false)
    }
  }

  const inputClass =
    'w-full rounded-xl border border-slate-200 px-3 py-3 text-sm text-slate-900 focus:border-sky-400 focus:outline-none focus:ring-4 focus:ring-sky-100'

  return (
    <div className="flex min-h-[70vh] items-center justify-center p-6">
      <div className="w-full max-w-[440px] rounded-[18px] border border-slate-200 bg-white p-8 shadow-[0_18px_45px_rgba(15,23,42,0.08)]">
        <h1 className="text-xl font-extrabold text-slate-900">เปลี่ยนรหัสผ่าน</h1>
        {forced ? (
          <p className="mt-2 rounded-xl border border-amber-100 bg-amber-50 px-3 py-2 text-sm text-amber-700">
            บัญชีนี้ใช้รหัสผ่านชั่วคราวที่ผู้ดูแลตั้งให้ — ต้องเปลี่ยนรหัสผ่านก่อนเริ่มใช้งานระบบ
          </p>
        ) : (
          <p className="mt-1 text-sm text-slate-500">
            ผู้ใช้: {user?.displayName || user?.username}
          </p>
        )}

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-500">รหัสผ่านปัจจุบัน</label>
            <input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              className={inputClass}
              autoComplete="current-password"
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-500">รหัสผ่านใหม่</label>
            <input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className={inputClass}
              autoComplete="new-password"
              required
            />
            <p className="mt-1 text-xs text-slate-400">อย่างน้อย 8 ตัวอักษร มีทั้งตัวอักษรและตัวเลข</p>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-500">ยืนยันรหัสผ่านใหม่</label>
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className={inputClass}
              autoComplete="new-password"
              required
            />
          </div>

          {(policyProblem || error) && (
            <p className="rounded-xl border border-red-100 bg-red-50 px-3 py-2 text-sm font-medium text-red-600">
              {policyProblem || error}
            </p>
          )}

          <button type="submit" disabled={invalid} className="dc-btn w-full py-3 disabled:opacity-50">
            {saving ? 'กำลังบันทึก...' : 'บันทึกรหัสผ่านใหม่'}
          </button>
        </form>
      </div>
    </div>
  )
}
