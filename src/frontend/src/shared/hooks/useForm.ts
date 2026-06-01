import { useState } from 'react'

export function useForm<T extends object>(initial: T) {
  const [values, setValues] = useState<T>(initial)
  const [errors, setErrors] = useState<Record<string, string>>({})

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) {
    const { name, value, type } = e.target
    const target = e.target as HTMLInputElement & { valueAsNumber?: number }
    setValues((prev) => ({
      ...prev,
      [name]: type === 'number' ? (target.valueAsNumber ?? Number(value))
        : typeof (prev as Record<string, unknown>)[name] === 'number' ? Number(value)
        : value,
    }))
  }

  return { values, setValues, handleChange, errors, setErrors }
}
