import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'

const apiClient = axios.create({
  baseURL: '/api/v1',
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  const companyId = localStorage.getItem('companyId')
  if (token) config.headers.Authorization = `Bearer ${token}`
  if (companyId) config.headers['X-Company-Id'] = companyId
  return config
})

export function clearSession() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('user')
}

function goToLogin() {
  clearSession()
  if (window.location.pathname !== '/login') window.location.href = '/login'
}

// access token อายุสั้น → เมื่อหมดอายุให้ต่ออายุด้วย refresh token เงียบ ๆ แล้วยิงคำขอเดิมซ้ำ
// (เก็บ promise เดียวไว้ กันหลายคำขอที่ 401 พร้อมกันแย่งกัน refresh — ซึ่งจะทำให้ token ถูก rotate ทิ้ง)
let refreshPromise: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
  const refreshToken = localStorage.getItem('refreshToken')
  if (!refreshToken) throw new Error('no refresh token')

  // ใช้ axios ตัวเปล่า ไม่ผ่าน interceptor นี้ กัน loop
  const { data } = await axios.post('/api/v1/auth/refresh', { refreshToken })
  localStorage.setItem('token', data.token)
  localStorage.setItem('refreshToken', data.refreshToken)
  localStorage.setItem('user', JSON.stringify(data))
  return data.token as string
}

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetriableConfig | undefined
    const url = config?.url ?? ''
    const isAuthCall = url.includes('/auth/login') || url.includes('/auth/refresh')

    if (error.response?.status !== 401 || isAuthCall || !config || config._retried) {
      if (error.response?.status === 401 && !isAuthCall) goToLogin()
      return Promise.reject(error)
    }

    try {
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })
      const token = await refreshPromise
      config._retried = true
      config.headers.Authorization = `Bearer ${token}`
      return apiClient.request(config)
    } catch {
      goToLogin()
      return Promise.reject(error)
    }
  },
)

export default apiClient
