import { useState } from 'react';
import {
  FileText,
  Plus,
  Edit3,
  Trash2,
  Copy,
  Check,
  Layout,
  ChevronRight,
  ChevronDown,
} from 'lucide-react';

interface StatementRow {
  id: string;
  label: string;
  type: 'header' | 'accountRange' | 'total' | 'formula' | 'spacer';
  accountFrom?: string;
  accountTo?: string;
  formula?: string;
  indent: number;
  bold: boolean;
  underline: boolean;
}

interface StatementColumn {
  id: string;
  label: string;
  type: 'period' | 'ytd' | 'budget' | 'variance' | 'variancePercent';
  periodNumber?: number;
}

interface StatementLayout {
  id: string;
  name: string;
  statementType: string;
  description: string;
  version: number;
  isApproved: boolean;
  rows: StatementRow[];
  columns: StatementColumn[];
  suppressZero: boolean;
  roundToNearestDollar: boolean;
}

const statementTypes = ['BalanceSheet', 'IncomeStatement', 'CashFlow', 'TrialBalance', 'Custom'];

const defaultColumns: StatementColumn[] = [
  { id: '1', label: 'Current Period', type: 'period', periodNumber: 1 },
  { id: '2', label: 'YTD', type: 'ytd' },
  { id: '3', label: 'Budget', type: 'budget' },
  { id: '4', label: 'Variance $', type: 'variance' },
  { id: '5', label: 'Variance %', type: 'variancePercent' },
];

const sampleRows: StatementRow[] = [
  { id: '1', label: 'ASSETS', type: 'header', indent: 0, bold: true, underline: false },
  { id: '2', label: 'Current Assets', type: 'header', indent: 1, bold: true, underline: false },
  { id: '3', label: 'Cash and Cash Equivalents', type: 'accountRange', accountFrom: '1000', accountTo: '1099', indent: 2, bold: false, underline: false },
  { id: '4', label: 'Accounts Receivable', type: 'accountRange', accountFrom: '1100', accountTo: '1199', indent: 2, bold: false, underline: false },
  { id: '5', label: 'Inventory', type: 'accountRange', accountFrom: '1200', accountTo: '1299', indent: 2, bold: false, underline: false },
  { id: '6', label: 'Total Current Assets', type: 'total', indent: 1, bold: true, underline: true },
  { id: '7', label: '', type: 'spacer', indent: 0, bold: false, underline: false },
  { id: '8', label: 'Fixed Assets', type: 'header', indent: 1, bold: true, underline: false },
  { id: '9', label: 'Property & Equipment', type: 'accountRange', accountFrom: '1500', accountTo: '1599', indent: 2, bold: false, underline: false },
  { id: '10', label: 'Total Fixed Assets', type: 'total', indent: 1, bold: true, underline: true },
  { id: '11', label: 'TOTAL ASSETS', type: 'formula', formula: 'Row6 + Row10', indent: 0, bold: true, underline: true },
];

export default function StatementDesignerPage() {
  const [layouts, setLayouts] = useState<StatementLayout[]>([
    {
      id: '1', name: 'Standard Balance Sheet', statementType: 'BalanceSheet',
      description: 'Default balance sheet layout', version: 3, isApproved: true,
      rows: sampleRows, columns: defaultColumns, suppressZero: false, roundToNearestDollar: false,
    },
    {
      id: '2', name: 'Income Statement - Monthly', statementType: 'IncomeStatement',
      description: 'Monthly income statement with budget comparison', version: 1, isApproved: false,
      rows: sampleRows, columns: defaultColumns, suppressZero: true, roundToNearestDollar: false,
    },
  ]);

  const [selectedLayout, setSelectedLayout] = useState<StatementLayout | null>(layouts[0]);
  const [editingRow, setEditingRow] = useState<string | null>(null);
  const [showNewLayout, setShowNewLayout] = useState(false);
  const [newLayoutName, setNewLayoutName] = useState('');
  const [newLayoutType, setNewLayoutType] = useState('BalanceSheet');

  const handleAddRow = (afterIndex: number) => {
    if (!selectedLayout) return;
    const newRow: StatementRow = {
      id: Date.now().toString(),
      label: 'New Row',
      type: 'accountRange',
      indent: 0,
      bold: false,
      underline: false,
    };
    const newRows = [...selectedLayout.rows];
    newRows.splice(afterIndex + 1, 0, newRow);
    setSelectedLayout({ ...selectedLayout, rows: newRows });
  };

  const handleDeleteRow = (rowId: string) => {
    if (!selectedLayout) return;
    setSelectedLayout({
      ...selectedLayout,
      rows: selectedLayout.rows.filter((r) => r.id !== rowId),
    });
  };

  const handleUpdateRow = (rowId: string, updates: Partial<StatementRow>) => {
    if (!selectedLayout) return;
    setSelectedLayout({
      ...selectedLayout,
      rows: selectedLayout.rows.map((r) => (r.id === rowId ? { ...r, ...updates } : r)),
    });
  };

  const handleCreateLayout = () => {
    if (!newLayoutName.trim()) return;
    const newLayout: StatementLayout = {
      id: Date.now().toString(),
      name: newLayoutName,
      statementType: newLayoutType,
      description: '',
      version: 1,
      isApproved: false,
      rows: [],
      columns: defaultColumns,
      suppressZero: false,
      roundToNearestDollar: false,
    };
    setLayouts([...layouts, newLayout]);
    setSelectedLayout(newLayout);
    setShowNewLayout(false);
    setNewLayoutName('');
  };

  const handleDuplicateLayout = (layout: StatementLayout) => {
    const duplicate: StatementLayout = {
      ...layout,
      id: Date.now().toString(),
      name: `${layout.name} (Copy)`,
      version: 1,
      isApproved: false,
    };
    setLayouts([...layouts, duplicate]);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Financial Statement Designer</h1>
          <p className="text-sm text-gray-500">Create and manage financial statement layouts with row/column/formula definitions</p>
        </div>
        <button
          onClick={() => setShowNewLayout(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          <Plus size={16} /> New Layout
        </button>
      </div>

      {showNewLayout && (
        <div className="bg-white rounded-lg border p-4 space-y-3">
          <h3 className="font-semibold">New Statement Layout</h3>
          <div className="grid grid-cols-3 gap-3">
            <input
              placeholder="Layout name"
              value={newLayoutName}
              onChange={(e) => setNewLayoutName(e.target.value)}
              className="border rounded px-3 py-2"
            />
            <select
              value={newLayoutType}
              onChange={(e) => setNewLayoutType(e.target.value)}
              className="border rounded px-3 py-2"
            >
              {statementTypes.map((t) => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
            <div className="flex gap-2">
              <button onClick={handleCreateLayout} className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">Create</button>
              <button onClick={() => setShowNewLayout(false)} className="px-4 py-2 bg-gray-200 rounded hover:bg-gray-300">Cancel</button>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-4 gap-6">
        {/* Layout List */}
        <div className="bg-white rounded-lg border">
          <div className="p-3 border-b font-semibold text-sm text-gray-600">Statement Layouts</div>
          <div className="divide-y max-h-[600px] overflow-y-auto">
            {layouts.map((layout) => (
              <div
                key={layout.id}
                className={`p-3 cursor-pointer hover:bg-gray-50 ${selectedLayout?.id === layout.id ? 'bg-blue-50 border-l-2 border-l-blue-500' : ''}`}
                onClick={() => setSelectedLayout(layout)}
              >
                <div className="flex items-center justify-between">
                  <div>
                    <div className="font-medium text-sm flex items-center gap-1">
                      <Layout size={14} />
                      {layout.name}
                    </div>
                    <div className="text-xs text-gray-500 mt-1">{layout.statementType} • v{layout.version}</div>
                  </div>
                  <div className="flex gap-1">
                    {layout.isApproved && <Check size={12} className="text-green-500" />}
                    <button
                      onClick={(e) => { e.stopPropagation(); handleDuplicateLayout(layout); }}
                      className="p-1 hover:bg-gray-200 rounded"
                    >
                      <Copy size={12} />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Row Editor */}
        <div className="col-span-3 bg-white rounded-lg border">
          {selectedLayout ? (
            <>
              <div className="p-3 border-b flex items-center justify-between">
                <div>
                  <h3 className="font-semibold">{selectedLayout.name}</h3>
                  <span className="text-xs text-gray-500">
                    {selectedLayout.rows.length} rows • {selectedLayout.columns.length} columns • Version {selectedLayout.version}
                  </span>
                </div>
                <div className="flex gap-2 items-center">
                  <label className="flex items-center gap-1 text-xs">
                    <input
                      type="checkbox"
                      checked={selectedLayout.suppressZero}
                      onChange={(e) => setSelectedLayout({ ...selectedLayout, suppressZero: e.target.checked })}
                    />
                    Suppress Zero
                  </label>
                  <label className="flex items-center gap-1 text-xs">
                    <input
                      type="checkbox"
                      checked={selectedLayout.roundToNearestDollar}
                      onChange={(e) => setSelectedLayout({ ...selectedLayout, roundToNearestDollar: e.target.checked })}
                    />
                    Round to $1
                  </label>
                  <button className="px-3 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700">
                    Run Preview
                  </button>
                </div>
              </div>

              {/* Column Headers */}
              <div className="flex border-b bg-gray-50 text-xs font-medium">
                <div className="w-8 px-2 py-2">#</div>
                <div className="flex-1 px-2 py-2">Row Label</div>
                <div className="w-24 px-2 py-2">Type</div>
                <div className="w-24 px-2 py-2">Account From</div>
                <div className="w-24 px-2 py-2">Account To</div>
                <div className="w-20 px-2 py-2">Indent</div>
                <div className="w-20 px-2 py-2">Style</div>
                <div className="w-24 px-2 py-2">Actions</div>
              </div>

              {/* Rows */}
              <div className="divide-y max-h-[500px] overflow-y-auto">
                {selectedLayout.rows.map((row, index) => (
                  <div key={row.id} className="flex items-center hover:bg-gray-50 group">
                    <div className="w-8 px-2 py-2 text-xs text-gray-400">{index + 1}</div>
                    <div className="flex-1 px-2 py-1" style={{ paddingLeft: `${row.indent * 20 + 8}px` }}>
                      {editingRow === row.id ? (
                        <input
                          autoFocus
                          value={row.label}
                          onChange={(e) => handleUpdateRow(row.id, { label: e.target.value })}
                          onBlur={() => setEditingRow(null)}
                          onKeyDown={(e) => e.key === 'Enter' && setEditingRow(null)}
                          className="w-full border rounded px-2 py-1 text-sm"
                        />
                      ) : (
                        <span
                          className={`text-sm cursor-pointer ${row.bold ? 'font-bold' : ''} ${row.underline ? 'underline' : ''}`}
                          onClick={() => setEditingRow(row.id)}
                        >
                          {row.label || '(empty)'}
                        </span>
                      )}
                    </div>
                    <div className="w-24 px-2 py-2">
                      <select
                        value={row.type}
                        onChange={(e) => handleUpdateRow(row.id, { type: e.target.value as StatementRow['type'] })}
                        className="text-xs border rounded px-1 py-1 w-full"
                      >
                        <option value="header">Header</option>
                        <option value="accountRange">Account Range</option>
                        <option value="total">Total</option>
                        <option value="formula">Formula</option>
                        <option value="spacer">Spacer</option>
                      </select>
                    </div>
                    <div className="w-24 px-2 py-1">
                      {row.type === 'accountRange' && (
                        <input
                          value={row.accountFrom || ''}
                          onChange={(e) => handleUpdateRow(row.id, { accountFrom: e.target.value })}
                          className="text-xs border rounded px-1 py-1 w-full"
                          placeholder="From"
                        />
                      )}
                    </div>
                    <div className="w-24 px-2 py-1">
                      {row.type === 'accountRange' && (
                        <input
                          value={row.accountTo || ''}
                          onChange={(e) => handleUpdateRow(row.id, { accountTo: e.target.value })}
                          className="text-xs border rounded px-1 py-1 w-full"
                          placeholder="To"
                        />
                      )}
                    </div>
                    <div className="w-20 px-2 py-1">
                      <select
                        value={row.indent}
                        onChange={(e) => handleUpdateRow(row.id, { indent: parseInt(e.target.value) })}
                        className="text-xs border rounded px-1 py-1 w-full"
                      >
                        {[0, 1, 2, 3, 4].map((n) => (
                          <option key={n} value={n}>{n}</option>
                        ))}
                      </select>
                    </div>
                    <div className="w-20 px-2 py-2 flex gap-1">
                      <button
                        onClick={() => handleUpdateRow(row.id, { bold: !row.bold })}
                        className={`text-xs px-1 rounded ${row.bold ? 'bg-gray-800 text-white' : 'bg-gray-200'}`}
                      >
                        B
                      </button>
                      <button
                        onClick={() => handleUpdateRow(row.id, { underline: !row.underline })}
                        className={`text-xs px-1 rounded ${row.underline ? 'bg-gray-800 text-white' : 'bg-gray-200'}`}
                      >
                        U
                      </button>
                    </div>
                    <div className="w-24 px-2 py-2 flex gap-1 opacity-0 group-hover:opacity-100">
                      <button
                        onClick={() => handleAddRow(index)}
                        className="p-1 hover:bg-gray-200 rounded"
                        title="Insert row below"
                      >
                        <Plus size={12} />
                      </button>
                      <button
                        onClick={() => handleDeleteRow(row.id)}
                        className="p-1 hover:bg-red-100 rounded text-red-500"
                        title="Delete row"
                      >
                        <Trash2 size={12} />
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              <div className="p-3 border-t bg-gray-50 text-xs text-gray-500">
                Click label to edit • Drag to reorder (coming soon) • Account Range rows pull GL balances • Total rows sum child rows • Formula rows support arithmetic
              </div>
            </>
          ) : (
            <div className="p-12 text-center text-gray-400">
              <FileText size={48} className="mx-auto mb-4" />
              <p>Select a layout to edit or create a new one</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
