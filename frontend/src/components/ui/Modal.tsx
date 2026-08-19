import { type ReactNode } from 'react'
import { X } from 'lucide-react'
import { cn } from '@utils/helpers'
import { Button } from './Button'
import { createPortal } from 'react-dom'

export interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  description?: string
  children: ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full'
  showCloseButton?: boolean
  closeOnOverlayClick?: boolean
  className?: string
  footer?: ReactNode
}

const sizeClasses = {
  sm: 'max-w-md',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl',
  full: 'max-w-[90vw]',
}

export const Modal = ({
  isOpen,
  onClose,
  title,
  description,
  children,
  size = 'md',
  showCloseButton = true,
  closeOnOverlayClick = true,
  className,
  footer,
}: ModalProps) => {
  if (!isOpen) return null

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget && closeOnOverlayClick) {
      onClose()
    }
  }

  const modalContent = (
    <div
      className={cn('fixed inset-0 z-modal flex items-center justify-center p-4', className)}
      onClick={handleOverlayClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? 'modal-title' : undefined}
      aria-describedby={description ? 'modal-description' : undefined}
    >
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm transition-opacity duration-fast"
        aria-hidden="true"
      />
      <div
        className={cn(
          'relative w-full bg-white dark:bg-gray-800 rounded-xl shadow-modal overflow-hidden',
          'transform transition-all duration-fast animate-in',
          sizeClasses[size]
        )}
      >
        {(title || showCloseButton) && (
          <div className="flex items-start justify-between p-4 border-b border-gray-200 dark:border-gray-700">
            <div>
              {title && (
                <h2 id="modal-title" className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                  {title}
                </h2>
              )}
              {description && (
                <p id="modal-description" className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  {description}
                </p>
              )}
            </div>
            {showCloseButton && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onClose}
                aria-label="Close modal"
                className="p-1 -m-1"
              >
                <X className="h-5 w-5" aria-hidden="true" />
              </Button>
            )}
          </div>
        )}
        <div className="p-4 max-h-[70vh] overflow-y-auto">{children}</div>
        {footer && (
          <div className="flex items-center justify-end gap-3 p-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
            {footer}
          </div>
        )}
      </div>
    </div>
  )

  if (typeof window === 'undefined') return null

  return createPortal(modalContent, document.body)
}

export interface ConfirmDialogProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => void
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  variant?: 'danger' | 'primary'
  isLoading?: boolean
}

export const ConfirmDialog = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  variant = 'primary',
  isLoading = false,
}: ConfirmDialogProps) => (
  <Modal 
    isOpen={isOpen} 
    onClose={onClose} 
    title={title} 
    size="sm"
    footer={
      <>
        <Button variant="secondary" onClick={onClose} disabled={isLoading}>
          {cancelText}
        </Button>
        <Button variant={variant === 'danger' ? 'destructive' : variant} onClick={onConfirm} isLoading={isLoading}>
          {confirmText}
        </Button>
      </>
    }
  >
    <p className="text-gray-600 dark:text-gray-400">{message}</p>
  </Modal>
)

export interface DrawerProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  children: ReactNode
  position?: 'right' | 'left' | 'bottom'
  size?: 'sm' | 'md' | 'lg' | 'full'
  className?: string
}

const positionClasses = {
  right: 'right-0',
  left: 'left-0',
  bottom: 'bottom-0 inset-x-0',
}

const sizeClassesDrawer = {
  sm: 'w-64',
  md: 'w-96',
  lg: 'w-[32rem]',
  full: 'w-full max-w-[90vw]',
}

export const Drawer = ({
  isOpen,
  onClose,
  title,
  children,
  position = 'right',
  size = 'md',
  className,
}: DrawerProps) => {
  if (!isOpen) return null

  const drawerContent = (
    <div
      className="fixed inset-0 z-modal flex"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? 'drawer-title' : undefined}
    >
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm transition-opacity duration-fast"
        aria-hidden="true"
      />
      <div
        className={cn(
          'relative flex flex-col bg-white dark:bg-gray-800 shadow-drawer overflow-hidden',
          'transform transition-all duration-fast animate-in',
          positionClasses[position],
          sizeClassesDrawer[size],
          className
        )}
        onClick={e => e.stopPropagation()}
      >
        {(title) && (
          <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
            <h2 id="drawer-title" className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              {title}
            </h2>
            <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close drawer">
              <X className="h-5 w-5" aria-hidden="true" />
            </Button>
          </div>
        )}
        <div className="flex-1 overflow-y-auto p-4">{children}</div>
      </div>
    </div>
  )

  if (typeof window === 'undefined') return null

  return createPortal(drawerContent, document.body)
}