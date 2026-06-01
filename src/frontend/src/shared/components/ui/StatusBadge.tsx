import type { ReactNode } from 'react'

type StatusBadgeTone = 'gray' | 'blue' | 'green' | 'red' | 'yellow'

interface StatusBadgeProps {
  tone?: StatusBadgeTone
  children: ReactNode
}

const toneClass: Record<StatusBadgeTone, string> = {
  gray: 'bg-gray-100 text-gray-500',
  blue: 'bg-blue-100 text-blue-700',
  green: 'bg-green-100 text-green-700',
  red: 'bg-red-100 text-red-700',
  yellow: 'bg-yellow-100 text-yellow-700',
}

export default function StatusBadge({ tone = 'gray', children }: StatusBadgeProps) {
  return (
    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${toneClass[tone]}`}>
      {children}
    </span>
  )
}
