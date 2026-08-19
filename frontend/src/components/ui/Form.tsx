import { forwardRef, type HTMLAttributes, type InputHTMLAttributes, type SelectHTMLAttributes, type TextareaHTMLAttributes, type ReactNode } from 'react'
import { cn, generateId } from '@utils/helpers'

export interface FormFieldProps extends HTMLAttributes<HTMLDivElement> {
  label?: string
  required?: boolean
  error?: string
  hint?: string
  children: ReactNode
}

export const FormField = forwardRef<HTMLDivElement, FormFieldProps>(
  ({ className, label, required = false, error, hint, children, id, ...props }, ref) => {
    const fieldId = id || `field-${generateId()}`
    const errorId = `${fieldId}-error`
    const hintId = `${fieldId}-hint`

    return (
      <div ref={ref} className={cn('w-full', className)} {...props}>
        {label && (
          <label
            htmlFor={fieldId}
            className={cn(
              'block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5',
              required && 'text-red-500'
            )}
          >
            {label}
            {required && <span className="text-red-500 ml-1" aria-hidden="true">*</span>}
          </label>
        )}
        <div>{children}</div>
        {error && (
          <p id={errorId} className="mt-1.5 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
        {hint && !error && (
          <p id={hintId} className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    )
  }
)

FormField.displayName = 'FormField'

export interface FormSectionProps extends HTMLAttributes<HTMLFieldSetElement> {
  title?: string
  description?: string
}

export const FormSection = forwardRef<HTMLFieldSetElement, FormSectionProps>(
  ({ className, title, description, children, ...props }, ref) => (
    <fieldset ref={ref} className={cn('border border-gray-200 dark:border-gray-700 rounded-lg p-4', className)} {...props}>
      {(title || description) && (
        <legend className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-4">
          {title}
          {description && <p className="text-sm font-normal text-gray-500 dark:text-gray-400 mt-1">{description}</p>}
        </legend>
      )}
      <div>{children}</div>
    </fieldset>
  )
)

FormSection.displayName = 'FormSection'

export interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label?: string
  error?: string
  hint?: string
  leftIcon?: ReactNode
  rightIcon?: ReactNode
  leftAddon?: string
  rightAddon?: string
  size?: 'sm' | 'md' | 'lg'
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  (
    {
      className,
      label,
      error,
      hint,
      leftIcon,
      rightIcon,
      leftAddon,
      rightAddon,
      size = 'md',
      id,
      ...props
    },
    ref
  ) => {
    const inputId = id || `input-${generateId()}`
    const hasLeftAddon = leftIcon || leftAddon
    const hasRightAddon = rightIcon || rightAddon

    const sizeClasses = {
      sm: { input: 'px-3 py-1.5 text-xs', left: 'pl-8', right: 'pr-8' },
      md: { input: 'px-4 py-2 text-sm', left: 'pl-10', right: 'pr-10' },
      lg: { input: 'px-4 py-3 text-base', left: 'pl-12', right: 'pr-12' },
    }

    const sizes = sizeClasses[size]

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={inputId}
            className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
          >
            {label}
            {props.required && <span className="text-red-500 ml-1" aria-hidden="true">*</span>}
          </label>
        )}
        <div className="relative">
          {hasLeftAddon && (
            <div className="absolute inset-y-0 left-0 flex items-center pointer-events-none text-gray-400 dark:text-gray-500">
              {leftAddon ? (
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300 px-3">{leftAddon}</span>
              ) : (
                <span className="pl-3">{leftIcon}</span>
              )}
            </div>
          )}
          <input
            ref={ref}
            id={inputId}
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
              hasLeftAddon ? sizes.left : 'pl-4',
              hasRightAddon ? sizes.right : 'pr-4',
              sizes.input,
              className
            )}
            aria-invalid={error ? 'true' : 'false'}
            aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
            {...props}
          />
          {hasRightAddon && (
            <div className="absolute inset-y-0 right-0 flex items-center pointer-events-none text-gray-400 dark:text-gray-500">
              {rightAddon ? (
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300 pr-3">{rightAddon}</span>
              ) : (
                <span className="pr-3">{rightIcon}</span>
              )}
            </div>
          )}
        </div>
        {error && (
          <p id={`${inputId}-error`} className="mt-1.5 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
        {hint && !error && (
          <p id={`${inputId}-hint`} className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    )
  }
)

Input.displayName = 'Input'

export interface TextareaProps extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'size'> {
  label?: string
  error?: string
  hint?: string
  size?: 'sm' | 'md' | 'lg'
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, label, error, hint, size = 'md', id, ...props }, ref) => {
    const textareaId = id || `textarea-${generateId()}`

    const sizeClasses = {
      sm: 'px-3 py-2 text-xs min-h-[60px]',
      md: 'px-4 py-2.5 text-sm min-h-[80px]',
      lg: 'px-4 py-3 text-base min-h-[100px]',
    }

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={textareaId}
            className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
          >
            {label}
            {props.required && <span className="text-red-500 ml-1" aria-hidden="true">*</span>}
          </label>
        )}
        <textarea
          ref={ref}
          id={textareaId}
          className={cn(
            'w-full rounded-lg border transition-colors duration-fast resize-y',
            'bg-white dark:bg-gray-800',
            'text-gray-900 dark:text-gray-100',
            'placeholder:text-gray-400 dark:placeholder:text-gray-500',
            'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
            'disabled:bg-gray-50 dark:disabled:bg-gray-900 disabled:text-gray-500 dark:disabled:text-gray-400 disabled:cursor-not-allowed',
            'hover:border-gray-400 dark:hover:border-gray-600',
            error
              ? 'border-red-500 focus:ring-red-500'
              : 'border-gray-300 dark:border-gray-600',
            sizeClasses[size],
            className
          )}
          aria-invalid={error ? 'true' : 'false'}
          aria-describedby={error ? `${textareaId}-error` : hint ? `${textareaId}-hint` : undefined}
          {...props}
        />
        {error && (
          <p id={`${textareaId}-error`} className="mt-1.5 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
        {hint && !error && (
          <p id={`${textareaId}-hint`} className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    )
  }
)

Textarea.displayName = 'Textarea'

export interface SelectOption {
  value: string
  label: string
  disabled?: boolean
}

export interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'size'> {
  label?: string
  error?: string
  hint?: string
  options: SelectOption[]
  placeholder?: string
  size?: 'sm' | 'md' | 'lg'
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, label, error, hint, options, placeholder, size = 'md', id, ...props }, ref) => {
    const selectId = id || `select-${generateId()}`

    const sizeClasses = {
      sm: 'px-3 py-1.5 text-xs pr-8',
      md: 'px-4 py-2 text-sm pr-10',
      lg: 'px-4 py-3 text-base pr-10',
    }

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={selectId}
            className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
          >
            {label}
            {props.required && <span className="text-red-500 ml-1" aria-hidden="true">*</span>}
          </label>
        )}
        <select
          ref={ref}
          id={selectId}
          className={cn(
            'w-full rounded-lg border transition-colors duration-fast appearance-none',
            'bg-white dark:bg-gray-800',
            'text-gray-900 dark:text-gray-100',
            'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
            'disabled:bg-gray-50 dark:disabled:bg-gray-900 disabled:text-gray-500 dark:disabled:text-gray-400 disabled:cursor-not-allowed',
            'hover:border-gray-400 dark:hover:border-gray-600',
            error
              ? 'border-red-500 focus:ring-red-500'
              : 'border-gray-300 dark:border-gray-600',
            sizeClasses[size],
            className
          )}
          aria-invalid={error ? 'true' : 'false'}
          aria-describedby={error ? `${selectId}-error` : hint ? `${selectId}-hint` : undefined}
          {...props}
        >
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map(option => (
            <option key={option.value} value={option.value} disabled={option.disabled}>
              {option.label}
            </option>
          ))}
        </select>
        {error && (
          <p id={`${selectId}-error`} className="mt-1.5 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
        {hint && !error && (
          <p id={`${selectId}-hint`} className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    )
  }
)

Select.displayName = 'Select'

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string
  description?: string
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, label, description, id, ...props }, ref) => {
    const checkboxId = id || `checkbox-${generateId()}`

    return (
      <div className="flex items-start gap-3">
        <input
          ref={ref}
          type="checkbox"
          id={checkboxId}
          className={cn(
            'h-4 w-4 rounded border-gray-300 text-primary-600',
            'focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 focus:ring-offset-white dark:focus:ring-offset-gray-900',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'transition-colors duration-fast',
            className
          )}
          {...props}
        />
        <div className="flex flex-col">
          <label htmlFor={checkboxId} className="text-sm font-medium text-gray-900 dark:text-gray-100 cursor-pointer">
            {label}
          </label>
          {description && (
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{description}</p>
          )}
        </div>
      </div>
    )
  }
)

Checkbox.displayName = 'Checkbox'

export interface RadioGroupProps extends Omit<HTMLAttributes<HTMLDivElement>, 'onChange'> {
  label?: string
  error?: string
  hint?: string
  options: { value: string; label: string; disabled?: boolean }[]
  value?: string
  onChange: (value: string) => void
  direction?: 'horizontal' | 'vertical'
}

export const RadioGroup = forwardRef<HTMLDivElement, RadioGroupProps>(
  ({ className, label, error, hint, options, value, onChange, direction = 'vertical', id, ...props }, ref) => {
    const groupId = id || `radiogroup-${generateId()}`

    return (
      <div ref={ref} className={cn('w-full', className)} {...props}>
        {label && (
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            {label}
          </label>
        )}
        <div
          role="radiogroup"
          aria-label={label}
          aria-describedby={error ? `${groupId}-error` : hint ? `${groupId}-hint` : undefined}
          className={cn(direction === 'horizontal' ? 'flex flex-wrap gap-4' : 'space-y-2')}
        >
          {options.map(option => (
            <label
              key={option.value}
              className={cn(
                'flex items-center gap-2 cursor-pointer',
                direction === 'horizontal' && 'whitespace-nowrap'
              )}
            >
              <input
                type="radio"
                name={groupId}
                value={option.value}
                checked={value === option.value}
                onChange={() => onChange(option.value)}
                disabled={option.disabled}
                className={cn(
                  'h-4 w-4 text-primary-600 border-gray-300',
                  'focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 focus:ring-offset-white dark:focus:ring-offset-gray-900',
                  'disabled:opacity-50 disabled:cursor-not-allowed',
                  'transition-colors duration-fast'
                )}
                aria-disabled={option.disabled}
              />
              <span className="text-sm text-gray-900 dark:text-gray-100">{option.label}</span>
            </label>
          ))}
        </div>
        {error && (
          <p id={`${groupId}-error`} className="mt-1.5 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
        {hint && !error && (
          <p id={`${groupId}-hint`} className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    )
  }
)

RadioGroup.displayName = 'RadioGroup'

export interface SwitchProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string
  description?: string
}

export const Switch = forwardRef<HTMLInputElement, SwitchProps>(
  ({ className, label, description, id, ...props }, ref) => {
    const switchId = id || `switch-${generateId()}`

    return (
      <div className="flex items-center gap-3">
        <div className="relative inline-flex items-center">
          <input
            ref={ref}
            type="checkbox"
            id={switchId}
            role="switch"
            className={cn(
              'peer h-5 w-5 appearance-none rounded-full border-2 border-gray-300',
            'bg-white',
            'checked:border-primary-600 checked:bg-primary-600',
            'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 focus:ring-offset-white dark:focus:ring-offset-gray-900',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'transition-colors duration-fast',
            'after:content-[""] after:absolute after:top-[2px] after:left-[2px] after:h-4 after:w-4 after:rounded-full after:bg-white after:shadow after:transition-transform',
            'checked:after:translate-x-full dark:after:bg-gray-100',
            'dark:border-gray-600 dark:checked:border-primary-500 dark:checked:bg-primary-500',
            className
          )}
            {...props}
          />
        </div>
        <div className="flex flex-col">
          {label && (
            <label htmlFor={switchId} className="text-sm font-medium text-gray-900 dark:text-gray-100 cursor-pointer">
              {label}
            </label>
          )}
          {description && (
            <p className="text-sm text-gray-500 dark:text-gray-400">{description}</p>
          )}
        </div>
      </div>
    )
  }
)

Switch.displayName = 'Switch'