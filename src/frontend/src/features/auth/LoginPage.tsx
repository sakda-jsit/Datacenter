import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../../shared/services/apiClient'

export default function LoginPage() {
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    try {
      const { data } = await apiClient.post('/auth/login', { username, password })
      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify(data))
      navigate('/dashboard')
    } catch {
      setError('ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-[linear-gradient(180deg,#f8fbff_0%,#f5f7fb_42%,#f7f8fb_100%)] p-6">
      <div className="w-full max-w-[400px] rounded-[18px] border border-slate-200 bg-white p-8 shadow-[0_18px_45px_rgba(15,23,42,0.08)]">
        <div className="flex items-center gap-3">
          <span className="grid h-11 w-11 place-items-center rounded-[14px] bg-gradient-to-br from-sky-400 to-sky-300 text-sm font-extrabold tracking-wider text-white shadow-[0_12px_28px_rgba(56,189,248,0.30)]">
            JS
          </span>
          <span className="leading-tight">
            <strong className="block text-lg font-extrabold text-slate-900">Datacenter</strong>
            <small className="text-xs font-medium tracking-wider text-slate-500">ACCOUNTING OFFICE</small>
          </span>
        </div>
        <h1 className="mt-6 text-2xl font-extrabold text-slate-900">เข้าสู่ระบบ</h1>
        <p className="mt-1 text-sm text-slate-500">ระบบสำนักงานบัญชีหลายบริษัท</p>
        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-500">ชื่อผู้ใช้</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full rounded-xl border border-slate-200 px-3 py-3 text-sm text-slate-900 focus:border-sky-400 focus:outline-none focus:ring-4 focus:ring-sky-100"
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-500">รหัสผ่าน</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full rounded-xl border border-slate-200 px-3 py-3 text-sm text-slate-900 focus:border-sky-400 focus:outline-none focus:ring-4 focus:ring-sky-100"
              required
            />
          </div>
          {error && <p className="rounded-xl border border-red-100 bg-red-50 px-3 py-2 text-sm font-medium text-red-600">{error}</p>}
          <button type="submit" className="dc-btn w-full py-3">
            เข้าสู่ระบบ
          </button>
        </form>
      </div>
    </div>
  )
}
