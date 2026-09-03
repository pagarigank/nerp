// <copyright file="densityStore.ts" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'

export type DensityMode = 'comfortable' | 'compact'

interface DensityState {
  mode: DensityMode
  setMode: (mode: DensityMode) => void
  toggleMode: () => void
}

export const useDensityStore = create<DensityState>()(
  persist(
    (set, get) => ({
      mode: 'comfortable',
      setMode: (mode) => set({ mode }),
      toggleMode: () => set({ mode: get().mode === 'comfortable' ? 'compact' : 'comfortable' }),
    }),
    {
      name: 'erp-density-mode',
      storage: createJSONStorage(() => localStorage),
    }
  )
)

export const useDensity = () => useDensityStore(s => s.mode)
