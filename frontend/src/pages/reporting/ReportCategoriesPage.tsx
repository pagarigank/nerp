import { useState, useEffect, useCallback } from 'react'
import { FolderTree, Plus, Edit2, Trash2, ChevronRight, ChevronDown, GripVertical } from 'lucide-react'

interface Category {
  id: string
  name: string
  description?: string
  icon?: string
  sortOrder: number
  isActive: boolean
  children: Category[]
}

export function ReportCategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [editForm, setEditForm] = useState({ name: '', description: '', parentId: '', sortOrder: 0, icon: '' })

  const fetchCategories = useCallback(async () => {
    setLoading(true)
    try {
      const response = await fetch('/api/v1/reporting/categories?companyId=00000000-0000-0000-0000-000000000001')
      const data = await response.json()
      setCategories(data.data || [])
    } catch (err) {
      console.error('Failed to fetch categories:', err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { fetchCategories() }, [fetchCategories])

  const toggleExpanded = (id: string) => {
    setExpandedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const handleCreate = async () => {
    try {
      await fetch('/api/v1/reporting/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          companyId: '00000000-0000-0000-0000-000000000001',
          ...editForm
        })
      })
      setIsCreating(false)
      setEditForm({ name: '', description: '', parentId: '', sortOrder: 0, icon: '' })
      fetchCategories()
    } catch (err) {
      console.error('Failed to create category:', err)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this category?')) return
    try {
      await fetch(`/api/v1/reporting/categories/${id}`, { method: 'DELETE' })
      fetchCategories()
    } catch (err) {
      console.error('Failed to delete category:', err)
    }
  }

  const renderTree = (nodes: Category[], depth = 0) => {
    return nodes.map(node => (
      <div key={node.id} style={{ paddingLeft: `${depth * 24}px` }}>
        <div
          className={`flex items-center gap-2 px-3 py-2 rounded-lg cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 ${selectedCategory?.id === node.id ? 'bg-blue-50 dark:bg-blue-900/20' : ''}`}
          onClick={() => setSelectedCategory(node)}
        >
          {node.children.length > 0 ? (
            <button
              onClick={(e) => { e.stopPropagation(); toggleExpanded(node.id) }}
              className="p-0.5 hover:bg-gray-200 rounded"
            >
              {expandedIds.has(node.id) ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
            </button>
          ) : (
            <span className="w-6" />
          )}
          <GripVertical size={14} className="text-gray-400" />
          <FolderTree size={16} className={node.isActive ? 'text-blue-500' : 'text-gray-400'} />
          <span className="flex-1 font-medium text-sm">{node.name}</span>
          <span className="text-xs text-gray-500">{node.children.length} items</span>
          {!node.isActive && <span className="text-xs text-red-500">Inactive</span>}
          <button onClick={(e) => { e.stopPropagation(); handleDelete(node.id) }} className="p-1 hover:bg-red-100 rounded text-red-500">
            <Trash2 size={14} />
          </button>
        </div>
        {expandedIds.has(node.id) && renderTree(node.children, depth + 1)}
      </div>
    ))
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Report Categories</h1>
          <p className="text-gray-500 mt-1">Organize reports into folders and categories</p>
        </div>
        <button
          onClick={() => setIsCreating(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2"
        >
          <Plus size={16} /> New Category
        </button>
      </div>

      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2 bg-white dark:bg-gray-900 rounded-xl border p-4">
          {loading ? (
            <div className="text-center py-8 text-gray-500">Loading categories...</div>
          ) : categories.length === 0 ? (
            <div className="text-center py-12">
              <FolderTree size={48} className="mx-auto text-gray-300 mb-4" />
              <p className="text-gray-500">No categories yet. Create your first category to organize reports.</p>
            </div>
          ) : (
            <div className="space-y-1">{renderTree(categories)}</div>
          )}
        </div>

        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          {isCreating ? (
            <div className="space-y-4">
              <h3 className="font-semibold">New Category</h3>
              <input
                type="text"
                placeholder="Category name"
                value={editForm.name}
                onChange={e => setEditForm({ ...editForm, name: e.target.value })}
                className="w-full px-3 py-2 border rounded-lg text-sm"
              />
              <textarea
                placeholder="Description (optional)"
                value={editForm.description}
                onChange={e => setEditForm({ ...editForm, description: e.target.value })}
                className="w-full px-3 py-2 border rounded-lg text-sm"
                rows={3}
              />
              <input
                type="number"
                placeholder="Sort order"
                value={editForm.sortOrder}
                onChange={e => setEditForm({ ...editForm, sortOrder: parseInt(e.target.value) || 0 })}
                className="w-full px-3 py-2 border rounded-lg text-sm"
              />
              <div className="flex gap-2">
                <button onClick={handleCreate} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm">Create</button>
                <button onClick={() => setIsCreating(false)} className="px-4 py-2 border rounded-lg text-sm">Cancel</button>
              </div>
            </div>
          ) : selectedCategory ? (
            <div className="space-y-3">
              <h3 className="font-semibold">Category Details</h3>
              <div className="text-sm space-y-2">
                <p><span className="text-gray-500">Name:</span> {selectedCategory.name}</p>
                <p><span className="text-gray-500">Description:</span> {selectedCategory.description || '—'}</p>
                <p><span className="text-gray-500">Sort Order:</span> {selectedCategory.sortOrder}</p>
                <p><span className="text-gray-500">Status:</span> {selectedCategory.isActive ? '✅ Active' : '❌ Inactive'}</p>
                <p><span className="text-gray-500">Children:</span> {selectedCategory.children.length}</p>
              </div>
            </div>
          ) : (
            <div className="text-center py-12 text-gray-400">
              <Edit2 size={32} className="mx-auto mb-2" />
              <p className="text-sm">Select a category to view details</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
