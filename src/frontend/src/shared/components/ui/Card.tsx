import type { HTMLAttributes, ReactNode } from 'react'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

export default function Card({ children, className = '', ...props }: CardProps) {
  return (
    <div className={`rounded-lg bg-white shadow ${className}`} {...props}>
      {children}
    </div>
  )
}
