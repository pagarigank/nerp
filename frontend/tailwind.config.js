/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Financial software color palette - restrained, professional
        primary: {
          50: '#f0f4f8',
          100: '#d9e2ec',
          200: '#bcccdc',
          300: '#9fb3c8',
          400: '#829ab1',
          500: '#627d98',
          600: '#486581',
          700: '#334e68',
          800: '#243b53',
          900: '#102a43',
          950: '#0c1d30',
        },
        // Status colors - used semantically, not decoratively
        status: {
          draft: '#9ca3af',      // gray-400
          pending: '#fbbf24',    // amber-400
          approved: '#3b82f6',   // blue-500
          posted: '#10b981',     // emerald-500
          voided: '#ef4444',     // red-500
        },
        // Semantic colors for financial data
        money: {
          positive: '#059669',   // emerald-600
          negative: '#dc2626',   // red-600
          neutral: '#374151',    // gray-700
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'Segoe UI', 'Roboto', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'SFMono-Regular', 'Consolas', 'monospace'],
        heading: ['Inter', 'system-ui', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      fontSize: {
        // Tabular numerals for financial data
        'tabular-sm': ['0.75rem', { fontVariantNumeric: 'tabular-nums', lineHeight: '1.5' }],
        'tabular-base': ['0.875rem', { fontVariantNumeric: 'tabular-nums', lineHeight: '1.5' }],
        'tabular-lg': ['1rem', { fontVariantNumeric: 'tabular-nums', lineHeight: '1.5' }],
        'tabular-xl': ['1.125rem', { fontVariantNumeric: 'tabular-nums', lineHeight: '1.5' }],
      },
      spacing: {
        '18': '4.5rem',
        '88': '22rem',
        '128': '32rem',
      },
      boxShadow: {
        'card': '0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)',
        'card-hover': '0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)',
        'modal': '0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)',
        'drawer': '0 -2px 10px rgb(0 0 0 / 0.1)',
      },
      borderRadius: {
        'xl': '0.75rem',
        '2xl': '1rem',
      },
      zIndex: {
        'nav': '100',
        'dropdown': '200',
        'modal': '300',
        'toast': '400',
        'tooltip': '500',
      },
      transitionDuration: {
        'fast': '100ms',
        'normal': '200ms',
        'slow': '300ms',
      },
    },
  },
  plugins: [],
}