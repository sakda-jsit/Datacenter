type StateMessageTone = 'muted' | 'error' | 'success' | 'warning'

interface StateMessageProps {
  children: string
  tone?: StateMessageTone
  centered?: boolean
}

const toneClass: Record<StateMessageTone, string> = {
  muted: 'text-gray-500',
  error: 'text-red-500',
  success: 'text-green-700',
  warning: 'text-amber-700',
}

export default function StateMessage({ children, tone = 'muted', centered = false }: StateMessageProps) {
  return (
    <p className={`text-sm ${toneClass[tone]} ${centered ? 'py-10 text-center' : ''}`}>
      {children}
    </p>
  )
}
