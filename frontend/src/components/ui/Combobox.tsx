import { Fragment } from 'react'
import { createPortal } from 'react-dom'
import { X, ChevronDown, ChevronUp } from 'lucide-react'
import { cn, generateId } from '@utils/helpers'
import { forwardRef, useState, useRef, useEffect, useMemo, useCallback, type ReactNode } from 'react'

export interface SelectOption {
  value: string
  label: string
  disabled?: boolean
  group?: string
}

export interface ComboboxProps {
  label?: string
  error?: string
  hint?: string
  placeholder?: string
  options: SelectOption[]
  value?: string | undefined
  onChange: (value: string) => void
  onSearch?: (query: string) => void
  isLoading?: boolean
  disabled?: boolean
  required?: boolean
  allowClear?: boolean
  groupBy?: (option: SelectOption) => string | undefined
  renderOption?: (option: SelectOption) => ReactNode
  renderSelected?: (option: SelectOption) => ReactNode
  className?: string
  id?: string
}

export const Combobox = forwardRef<HTMLDivElement, ComboboxProps>(
  (
    {
      className,
      label,
      error,
      hint,
      placeholder = 'Select...',
      options,
      value,
      onChange,
      onSearch,
      isLoading = false,
      disabled = false,
      required = false,
      allowClear = false,
      groupBy,
      renderOption,
      renderSelected,
      id,
    },
    ref
  ) => {
    const comboboxId = id || `combobox-${generateId()}`
    const [isOpen, setIsOpen] = useState(false)
    const [searchQuery, setSearchQuery] = useState('')
    const [highlightedIndex, setHighlightedIndex] = useState(-1)
    const inputRef = useRef<HTMLInputElement>(null)
    const listRef = useRef<HTMLUListElement>(null)
    const optionsRef = useRef<HTMLLIElement[]>([])

    const groupedOptions = useMemo(() => {
      if (!groupBy) return { '': options }
      const groups: Record<string, SelectOption[]> = {}
      options.forEach(option => {
        const group = groupBy(option) || ''
        if (!groups[group]) groups[group] = []
        groups[group].push(option)
      })
      return groups
    }, [options, groupBy])

    const filteredOptions = useMemo(() => {
      if (!searchQuery) return options
      const query = searchQuery.toLowerCase()
      return options.filter(
        opt =>
          opt.label.toLowerCase().includes(query) ||
          opt.value.toLowerCase().includes(query)
      )
    }, [options, searchQuery])

    const selectedOption = options.find(opt => opt.value === value)

    useEffect(() => {
      if (isOpen) {
        document.addEventListener('keydown', handleKeyDown)
        document.addEventListener('mousedown', handleClickOutside)
        inputRef.current?.focus()
        scrollToHighlighted()
      }
      return () => {
        document.removeEventListener('keydown', handleKeyDown)
        document.removeEventListener('mousedown', handleClickOutside)
      }
    }, [isOpen])

    const handleKeyDown = useCallback((e: KeyboardEvent) => {
      if (!isOpen) return

      const visibleOptions = filteredOptions.filter(opt => !opt.disabled)

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault()
          setHighlightedIndex(prev => Math.min(prev + 1, visibleOptions.length - 1))
          break
        case 'ArrowUp':
          e.preventDefault()
          setHighlightedIndex(prev => Math.max(prev - 1, 0))
          break
        case 'Enter':
          e.preventDefault()
          if (highlightedIndex >= 0 && visibleOptions[highlightedIndex]) {
            onChange(visibleOptions[highlightedIndex].value)
            setIsOpen(false)
            setSearchQuery('')
            setHighlightedIndex(-1)
          }
          break
        case 'Escape':
          e.preventDefault()
          setIsOpen(false)
          setSearchQuery('')
          setHighlightedIndex(-1)
          inputRef.current?.blur()
          break
        case 'Tab':
          setIsOpen(false)
          setSearchQuery('')
          setHighlightedIndex(-1)
          break
        default:
          break
      }
    }, [isOpen, filteredOptions, highlightedIndex, onChange])

    const handleClickOutside = useCallback((e: MouseEvent) => {
      const refObj = ref as React.RefObject<HTMLDivElement | null>
      if (refObj.current && !refObj.current.contains(e.target as Node)) {
        setIsOpen(false)
        setSearchQuery('')
        setHighlightedIndex(-1)
      }
    }, [ref])

    const scrollToHighlighted = useCallback(() => {
      if (highlightedIndex >= 0 && listRef.current && optionsRef.current[highlightedIndex]) {
        optionsRef.current[highlightedIndex].scrollIntoView({ block: 'nearest' })
      }
    }, [highlightedIndex])

    const handleOptionClick = (option: SelectOption) => {
      if (option.disabled) return
      onChange(option.value)
      setIsOpen(false)
      setSearchQuery('')
      setHighlightedIndex(-1)
      inputRef.current?.focus()
    }

    const handleClear = (e: React.MouseEvent) => {
      e.stopPropagation()
      onChange('')
      setSearchQuery('')
      inputRef.current?.focus()
    }

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const query = e.target.value
      setSearchQuery(query)
      setHighlightedIndex(-1)
      onSearch?.(query)
      if (!isOpen) setIsOpen(true)
    }

    const handleInputFocus = () => {
      if (!isOpen) setIsOpen(true)
    }

    return (
      <div
        ref={ref}
        className={cn('w-full', className)}
      >
        {label && (
          <label
            htmlFor={comboboxId}
            className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
          >
            {label}
            {required && <span className="text-red-500 ml-1" aria-hidden="true">*</span>}
          </label>
        )}
        <div className="relative">
          <div className="relative">
            <input
              ref={inputRef}
              id={comboboxId}
              type="text"
              autoComplete="off"
              role="combobox"
              aria-expanded={isOpen}
              aria-haspopup="listbox"
              aria-controls={`${comboboxId}-listbox`}
              aria-activedescendant={
                highlightedIndex >= 0 ? `${comboboxId}-option-${highlightedIndex}` : undefined
              }
              placeholder={placeholder}
              value={selectedOption ? (renderSelected ? '' : selectedOption.label) : searchQuery}
              onChange={handleInputChange}
              onFocus={handleInputFocus}
              onClick={() => setIsOpen(true)}
              disabled={disabled}
              className={cn(
                'w-full rounded-lg border transition-colors duration-fast',
                'bg-white dark:bg-gray-800',
                'text-gray-900 dark:text-gray-100',
                'placeholder:text-gray-400 dark:placeholder:text-gray-500',
                'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
                'disabled:bg-gray-50 dark:disabled:bg-gray-900 disabled:text-gray-500 dark:disabled:text-gray-400 disabled:cursor-not-allowed',
                'hover:border-gray-400 dark:hover:border-gray-600',
                error
                  ? 'border-red-500 focus:ring-red-500'
                  : 'border-gray-300 dark:border-gray-600',
                (allowClear || isLoading) ? 'pr-12' : 'pr-10',
                'pl-4 py-2.5 text-sm'
              )}
              aria-invalid={error ? 'true' : 'false'}
              aria-describedby={error ? `${comboboxId}-error` : hint ? `${comboboxId}-hint` : undefined}
            />
            <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
              {isLoading && (
                <svg className="animate-spin h-5 w-5 text-gray-400" viewBox="0 0 24 24">
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                    fill="none"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
              )}
              {!isLoading && (
                <>
                  {allowClear && value && (
                    <button
                      type="button"
                      onClick={handleClear}
                      className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
                      aria-label="Clear selection"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  )}
                  <ChevronDown className={cn('h-5 w-5 text-gray-400', isOpen && 'rotate-180')} />
                </>
              )}
            </div>
          </div>

          {isOpen && createPortal(
            <ul
              ref={listRef}
              id={`${comboboxId}-listbox`}
              role="listbox"
              aria-label={label}
              className="absolute z-dropdown w-full mt-1 max-h-60 overflow-auto rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-lg animate-in"
            >
              {Object.entries(groupedOptions).map(([group, groupOptions]) => {
                const filteredGroupOptions = groupOptions.filter(opt =>
                  !searchQuery ||
                  opt.label.toLowerCase().includes(searchQuery.toLowerCase()) ||
                  opt.value.toLowerCase().includes(searchQuery.toLowerCase())
                )

                if (filteredGroupOptions.length === 0) return null

                return (
                  <Fragment key={group}>
                    {group && (
                      <li className="px-3 py-1.5 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider bg-gray-50 dark:bg-gray-900/50 border-b border-gray-100 dark:border-gray-700">
                        {group}
                      </li>
                    )}
                    {filteredGroupOptions.map((option) => {
                      const globalIndex = options.indexOf(option)
                      const isHighlighted = highlightedIndex === globalIndex
                      const isSelected = option.value === value

                      return (
                        <li
                          key={option.value}
                          ref={el => { optionsRef.current[globalIndex] = el! }}
                          id={`${comboboxId}-option-${globalIndex}`}
                          role="option"
                          aria-selected={isSelected}
                          aria-disabled={option.disabled}
                          className={cn(
                            'px-3 py-2 cursor-pointer transition-colors',
                            'text-gray-900 dark:text-gray-100',
                            isHighlighted && 'bg-primary-50 dark:bg-primary-900/30',
                            isSelected && !isHighlighted && 'bg-primary-50 dark:bg-primary-900/20',
                            option.disabled && 'opacity-50 cursor-not-allowed'
                          )}
                          onClick={() => handleOptionClick(option)}
                          onMouseEnter={() => setHighlightedIndex(globalIndex)}
                        >
                          {renderOption ? (
                            renderOption(option)
                          ) : (
                            <div className="flex items-center justify-between">
                              <span>{option.label}</span>
                              {isSelected && (
                                <ChevronUp className="h-4 w-4 text-primary-500" aria-hidden="true" />
                              )}
                            </div>
                          )}
                        </li>
                      )
                    })}
                  </Fragment>
                )
              })}
              {filteredOptions.length === 0 && !isLoading && (
                <li className="px-3 py-4 text-center text-gray-500 dark:text-gray-400 text-sm">
                  No options found
                </li>
              )}
            </ul>,
            document.body
          )}
        </div>
        {error && (
          <p id={`${comboboxId}-error`} className="mt-1.5 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
        {hint && !error && (
          <p id={`${comboboxId}-hint`} className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    )
  }
)

Combobox.displayName = 'Combobox'