import { createContext, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'

interface CurrentCompanyContextValue {
  companyId: number
  selectCompany: (id: number) => void
}

const CurrentCompanyContext = createContext<CurrentCompanyContextValue | null>(null)

export function CurrentCompanyProvider({ children }: { children: ReactNode }) {
  const [companyId, setCompanyId] = useState(() => {
    try {
      const raw = localStorage.getItem('companyId')
      return raw ? Number(raw) : 0
    } catch {
      return 0
    }
  })

  const value = useMemo<CurrentCompanyContextValue>(() => ({
    companyId,
    selectCompany: (id: number) => {
      try {
        if (id > 0) localStorage.setItem('companyId', String(id))
        else localStorage.removeItem('companyId')
      } catch {
        // Ignore storage failures in restricted browser contexts.
      }
      setCompanyId(id)
    },
  }), [companyId])

  return (
    <CurrentCompanyContext.Provider value={value}>
      {children}
    </CurrentCompanyContext.Provider>
  )
}

export function useCurrentCompany() {
  const context = useContext(CurrentCompanyContext)
  if (!context) {
    throw new Error('useCurrentCompany must be used within CurrentCompanyProvider')
  }
  return context
}
