import type { ButtonHTMLAttributes, ReactNode } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  children: ReactNode
}

const variantClass: Record<ButtonVariant, string> = {
  primary: 'bg-slate-800 text-white hover:bg-slate-700 disabled:opacity-50',
  secondary: 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 disabled:opacity-50',
  danger: 'border border-red-200 bg-white text-red-600 hover:bg-red-50 disabled:opacity-50',
  ghost: 'text-sky-700 hover:bg-sky-50 disabled:opacity-50',
}

export default function Button({ variant = 'primary', className = '', children, ...props }: ButtonProps) {
  return (
    <button
      className={`inline-flex items-center justify-center rounded px-4 py-2 text-sm font-medium transition ${variantClass[variant]} ${className}`}
      {...props}
    >
      {children}
    </button>
  )
}
