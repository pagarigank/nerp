// <copyright file="navigationStore.ts" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'

interface NavHistoryEntry {
  to: string
  label: string
  module: string
  visitedAt: number
}

interface NavigationState {
  /** Set of favorited route paths (e.g. "/gl/journal-batches"). */
  favorites: string[]
  /** Map of route path → label for favorites display. */
  favoriteLabels: Record<string, string>
  /** Last 10 visited routes (most recent first). */
  recentVisits: NavHistoryEntry[]
  /** Max recent visits to keep. */
  maxRecent: number

  /** Toggle a page as favorite/unfavorite. */
  toggleFavorite: (path: string, label: string) => void
  /** Check if a page is favorited. */
  isFavorite: (path: string) => boolean
  /** Record a page visit. */
  recordVisit: (path: string, label: string, module: string) => void
  /** Get the most recent N visits. */
  getRecentVisits: (count?: number) => NavHistoryEntry[]
}

export const useNavigationStore = create<NavigationState>()(
  persist(
    (set, get) => ({
      favorites: [],
      favoriteLabels: {},
      recentVisits: [],
      maxRecent: 10,

      toggleFavorite: (path, label) => {
        const { favorites, favoriteLabels } = get()
        if (favorites.includes(path)) {
          set({
            favorites: favorites.filter(f => f !== path),
            favoriteLabels: Object.fromEntries(
              Object.entries(favoriteLabels).filter(([k]) => k !== path)
            ),
          })
        } else {
          set({
            favorites: [...favorites, path],
            favoriteLabels: { ...favoriteLabels, [path]: label },
          })
        }
      },

      isFavorite: (path) => get().favorites.includes(path),

      recordVisit: (path, label, module) => {
        const { recentVisits, maxRecent } = get()
        // Remove existing entry for this path
        const filtered = recentVisits.filter(v => v.to !== path)
        // Add new entry at the front
        const updated = [
          { to: path, label, module, visitedAt: Date.now() },
          ...filtered,
        ].slice(0, maxRecent)
        set({ recentVisits: updated })
      },

      getRecentVisits: (count = 5) => get().recentVisits.slice(0, count),
    }),
    {
      name: 'erp-navigation',
      storage: createJSONStorage(() => localStorage),
    }
  )
)

export const useFavorites = () => useNavigationStore(s => ({
  favorites: s.favorites,
  favoriteLabels: s.favoriteLabels,
  toggleFavorite: s.toggleFavorite,
  isFavorite: s.isFavorite,
}))

export const useRecentVisits = () => useNavigationStore(s => ({
  recentVisits: s.recentVisits,
  recordVisit: s.recordVisit,
  getRecentVisits: s.getRecentVisits,
}))
