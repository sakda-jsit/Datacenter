import { useEffect, useMemo, useRef, useState } from 'react'
import type { KeyboardEvent as ReactKeyboardEvent } from 'react'

type SelectValue = string | number

export interface SearchableSelectOption {
  value: SelectValue
  label: string
  searchText?: string
  disabled?: boolean
}

interface SearchableSelectProps {
  value: SelectValue
  options: SearchableSelectOption[]
  onChange: (value: SelectValue) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyMessage?: string
  className?: string
  disabled?: boolean
}

export default function SearchableSelect({
  value,
  options,
  onChange,
  placeholder = 'เลือกข้อมูล',
  searchPlaceholder = 'พิมพ์เพื่อค้นหา...',
  emptyMessage = 'ไม่พบรายการ',
  className = '',
  disabled = false,
}: SearchableSelectProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [activeIndex, setActiveIndex] = useState(0)
  const rootRef = useRef<HTMLDivElement | null>(null)
  const inputRef = useRef<HTMLInputElement | null>(null)
  const optionRefs = useRef<Array<HTMLButtonElement | null>>([])

  const selected = options.find((option) => String(option.value) === String(value))
  const filteredOptions = useMemo(() => {
    const term = query.trim().toLocaleLowerCase('th-TH')
    if (!term) return options

    return options.filter((option) => {
      const text = `${option.label} ${option.searchText ?? ''}`.toLocaleLowerCase('th-TH')
      return text.includes(term)
    })
  }, [options, query])

  useEffect(() => {
    if (!open) return
    inputRef.current?.focus()

    function handlePointerDown(event: PointerEvent) {
      const target = event.target
      if (!(target instanceof Node)) return
      if (rootRef.current?.contains(target)) return
      setOpen(false)
    }

  function handleKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('pointerdown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  useEffect(() => {
    if (!open) return
    setActiveIndex(0)
  }, [open, query])

  useEffect(() => {
    if (!open) return
    optionRefs.current[activeIndex]?.scrollIntoView({ block: 'nearest' })
  }, [activeIndex, open])

  function handleSelect(option: SearchableSelectOption) {
    if (option.disabled) return
    onChange(option.value)
    setOpen(false)
    setQuery('')
  }

  function moveActive(step: 1 | -1) {
    if (filteredOptions.length === 0) return

    setActiveIndex((current) => {
      let next = current
      for (let i = 0; i < filteredOptions.length; i += 1) {
        next = (next + step + filteredOptions.length) % filteredOptions.length
        if (!filteredOptions[next]?.disabled) return next
      }
      return current
    })
  }

  function handleSelectKeyDown(event: ReactKeyboardEvent<HTMLDivElement>) {
    if (!open && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
      event.preventDefault()
      setOpen(true)
      return
    }

    if (!open) return

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      moveActive(1)
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault()
      moveActive(-1)
    }

    if (event.key === 'Enter') {
      event.preventDefault()
      const activeOption = filteredOptions[activeIndex]
      if (activeOption) handleSelect(activeOption)
    }
  }

  return (
    <div ref={rootRef} className={`relative ${className}`} onKeyDown={handleSelectKeyDown}>
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((current) => !current)}
        className="flex w-full items-center justify-between gap-2 rounded border border-gray-300 bg-white px-3 py-2 text-left text-sm text-slate-700 transition focus:outline-none focus:ring-2 focus:ring-slate-400 disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-400"
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        <span className={`min-w-0 truncate ${selected ? '' : 'text-gray-400'}`}>
          {selected?.label ?? placeholder}
        </span>
        <span className={`text-xs text-slate-400 transition ${open ? 'rotate-180' : ''}`}>⌄</span>
      </button>

      {open && (
        <div className="absolute left-0 top-[calc(100%+6px)] z-50 w-full min-w-[240px] overflow-hidden rounded-lg border border-slate-200 bg-white shadow-[0_18px_45px_rgba(15,23,42,0.14)]">
          <div className="border-b border-slate-100 p-2">
            <input
              ref={inputRef}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={searchPlaceholder}
              className="w-full rounded border border-slate-200 px-3 py-2 text-sm outline-none focus:border-sky-300 focus:ring-2 focus:ring-sky-100"
            />
          </div>

          <div className="max-h-64 overflow-y-auto py-1" role="listbox">
            {filteredOptions.length === 0 && (
              <div className="px-3 py-3 text-sm text-gray-400">{emptyMessage}</div>
            )}

            {filteredOptions.map((option, index) => {
              const active = String(option.value) === String(value)
              const keyboardActive = index === activeIndex
              return (
                <button
                  key={String(option.value)}
                  ref={(element) => {
                    optionRefs.current[index] = element
                  }}
                  type="button"
                  disabled={option.disabled}
                  onClick={() => handleSelect(option)}
                  onMouseEnter={() => setActiveIndex(index)}
                  className={`flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm transition ${
                    active
                      ? 'bg-sky-50 text-sky-700'
                      : keyboardActive
                        ? 'bg-slate-100 text-slate-900'
                        : 'text-slate-700 hover:bg-slate-50'
                  } ${option.disabled ? 'cursor-not-allowed opacity-45' : ''}`}
                  role="option"
                  aria-selected={active}
                >
                  <span className="min-w-0 truncate">{option.label}</span>
                  {active && <span className="text-xs font-bold text-sky-600">✓</span>}
                </button>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
