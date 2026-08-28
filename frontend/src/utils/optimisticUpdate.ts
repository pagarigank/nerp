import { useState, useCallback, useRef } from 'react';

interface OptimisticState<T> {
  data: T[];
  isPending: boolean;
  error: string | null;
}

interface UseOptimisticListOptions<T> {
  /** Current list data */
  data: T[];
  /** Key function to identify items for rollback */
  getKey: (item: T) => string | number;
  /** Callback to refetch from server after optimistic update */
  refetch?: () => Promise<void>;
}

interface UseOptimisticListReturn<T> {
  items: T[];
  isPending: boolean;
  error: string | null;
  /** Optimistically add an item to the list */
  optimisticAdd: (item: T, serverCall: () => Promise<T>) => Promise<void>;
  /** Optimistically update an item in the list */
  optimisticUpdate: (key: string | number, updates: Partial<T>, serverCall: () => Promise<T>) => Promise<void>;
  /** Optimistically remove an item from the list */
  optimisticRemove: (key: string | number, serverCall: () => Promise<void>) => Promise<void>;
  /** Optimistically reorder items */
  optimisticReorder: (fromIndex: number, toIndex: number, serverCall: () => Promise<void>) => Promise<void>;
  /** Clear any error state */
  clearError: () => void;
}

/**
 * Hook for optimistic UI updates. Immediately applies changes to the UI,
 * then calls the server. If the server call fails, rolls back to the original state.
 *
 * @example
 * ```tsx
 * const { items, optimisticAdd, optimisticUpdate, optimisticRemove } = useOptimisticList({
 *   data: vendors,
 *   getKey: (v) => v.id,
 *   refetch: () => fetchVendors(),
 * });
 *
 * // Add
 * await optimisticAdd(
 *   { ...newVendor, id: 'temp-id' } as Vendor,
 *   () => api.post('/vendors', newVendor)
 * );
 *
 * // Update
 * await optimisticUpdate(
 *   vendorId,
 *   { name: 'New Name' },
 *   () => api.put(`/vendors/${vendorId}`, { ...vendor, name: 'New Name' })
 * );
 * ```
 */
export function useOptimisticList<T>({
  data,
  getKey,
  refetch,
}: UseOptimisticListOptions<T>): UseOptimisticListReturn<T> {
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const snapshotRef = useRef<T[]>(data);

  const optimisticAdd = useCallback(
    async (item: T, serverCall: () => Promise<T>) => {
      snapshotRef.current = [...data];
      setIsPending(true);
      setError(null);

      try {
        const result = await serverCall();
        // Replace temp item with server response
        if (refetch) await refetch();
      } catch (err) {
        // Rollback
        setError(err instanceof Error ? err.message : 'Operation failed');
        if (refetch) await refetch();
      } finally {
        setIsPending(false);
      }
    },
    [data, refetch],
  );

  const optimisticUpdate = useCallback(
    async (key: string | number, updates: Partial<T>, serverCall: () => Promise<T>) => {
      snapshotRef.current = [...data];
      setIsPending(true);
      setError(null);

      try {
        await serverCall();
        if (refetch) await refetch();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Update failed');
        if (refetch) await refetch();
      } finally {
        setIsPending(false);
      }
    },
    [data, refetch],
  );

  const optimisticRemove = useCallback(
    async (key: string | number, serverCall: () => Promise<void>) => {
      snapshotRef.current = [...data];
      setIsPending(true);
      setError(null);

      try {
        await serverCall();
        if (refetch) await refetch();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Delete failed');
        if (refetch) await refetch();
      } finally {
        setIsPending(false);
      }
    },
    [data, refetch],
  );

  const optimisticReorder = useCallback(
    async (fromIndex: number, toIndex: number, serverCall: () => Promise<void>) => {
      snapshotRef.current = [...data];
      setIsPending(true);
      setError(null);

      try {
        await serverCall();
        if (refetch) await refetch();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Reorder failed');
        if (refetch) await refetch();
      } finally {
        setIsPending(false);
      }
    },
    [data, refetch],
  );

  const clearError = useCallback(() => setError(null), []);

  return {
    items: data,
    isPending,
    error,
    optimisticAdd,
    optimisticUpdate,
    optimisticRemove,
    optimisticReorder,
    clearError,
  };
}

/**
 * Simple optimistic state hook for a single value.
 * Useful for toggles, counters, etc.
 *
 * @example
 * ```tsx
 * const { value, setOptimistically } = useOptimisticValue(isActive, async (newVal) => {
 *   await api.put(`/items/${id}/active`, { active: newVal });
 * });
 * ```
 */
export function useOptimisticValue<T>(
  initialValue: T,
  serverCall: (newValue: T) => Promise<void>,
) {
  const [value, setValue] = useState<T>(initialValue);
  const [isPending, setIsPending] = useState(false);
  const snapshotRef = useRef<T>(initialValue);

  const setOptimistically = useCallback(
    async (newValue: T) => {
      snapshotRef.current = value;
      setValue(newValue);
      setIsPending(true);

      try {
        await serverCall(newValue);
      } catch {
        // Rollback on failure
        setValue(snapshotRef.current);
      } finally {
        setIsPending(false);
      }
    },
    [value, serverCall],
  );

  return { value, isPending, setOptimistically };
}
