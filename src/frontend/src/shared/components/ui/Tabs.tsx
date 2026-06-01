import type { ReactNode } from 'react'

export interface TabItem<T extends string> {
  key: T
  label: ReactNode
}

interface TabsProps<T extends string> {
  items: TabItem<T>[]
  activeKey: T
  onChange: (key: T) => void
  className?: string
}

export default function Tabs<T extends string>({
  items,
  activeKey,
  onChange,
  className = '',
}: TabsProps<T>) {
  return (
    <div className={`mb-5 flex gap-1 border-b border-gray-200 ${className}`}>
      {items.map((item) => (
        <button
          key={item.key}
          type="button"
          onClick={() => onChange(item.key)}
          className={`rounded-t px-5 py-2.5 text-sm font-medium transition-colors ${
            activeKey === item.key
              ? 'border border-b-white border-gray-200 bg-white text-slate-800 -mb-px'
              : 'text-gray-500 hover:text-gray-700'
          }`}
        >
          {item.label}
        </button>
      ))}
    </div>
  )
}
