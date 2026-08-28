import { useState } from 'react';
import { Search, Save, Play, Download, Filter, Columns, Trash2, Clock, Star } from 'lucide-react';

interface QueryFilter {
  field: string;
  operator: string;
  value: string;
}

interface SavedQuery {
  id: string;
  name: string;
  entityName: string;
  filters: QueryFilter[];
  runCount: number;
  lastRunOn: string | null;
  isShared: boolean;
}

const entityOptions = [
  'JournalBatch', 'JournalBatchLine', 'Voucher', 'Payment',
  'Invoice', 'CashReceipt', 'SalesOrder', 'Shipment',
  'PurchaseOrder', 'Receipt', 'Item', 'Warehouse',
  'Project', 'CostTransaction', 'Employee', 'PayrollRun',
  'WorkOrder', 'BankAccount', 'Reconciliation',
];

const operatorOptions = ['Equals', 'Not Equals', 'Contains', 'Greater Than', 'Less Than',
  'Greater or Equal', 'Less or Equal', 'Starts With', 'In List', 'Between', 'Is Null', 'Is Not Null'];

const fieldOptions: Record<string, string[]> = {
  JournalBatch: ['BatchNumber', 'Status', 'Description', 'Period', 'TotalDebit', 'TotalCredit', 'CreatedOn'],
  SalesOrder: ['OrderNumber', 'CustomerName', 'Status', 'OrderDate', 'TotalAmount', 'SalesRep'],
  PurchaseOrder: ['PONumber', 'VendorName', 'Status', 'OrderDate', 'TotalAmount', 'Buyer'],
  Invoice: ['InvoiceNumber', 'CustomerName', 'Status', 'InvoiceDate', 'DueDate', 'TotalAmount'],
  Item: ['ItemCode', 'Description', 'ItemType', 'Status', 'StandardCost', 'OnHandQuantity'],
  Project: ['ProjectCode', 'Name', 'Status', 'ProjectManager', 'BudgetAmount', 'ActualCost'],
  Employee: ['EmployeeCode', 'FirstName', 'LastName', 'Department', 'Status', 'HireDate'],
  Payment: ['PaymentNumber', 'VendorName', 'PaymentDate', 'Amount', 'PaymentMethod'],
};

export default function QuickQueryPage() {
  const [selectedEntity, setSelectedEntity] = useState('');
  const [filters, setFilters] = useState<QueryFilter[]>([]);
  const [results, setResults] = useState<Record<string, unknown>[]>([]);
  const [savedQueries, setSavedQueries] = useState<SavedQuery[]>([
    { id: '1', name: 'Open POs over $10K', entityName: 'PurchaseOrder', filters: [{ field: 'Status', operator: 'Equals', value: 'Approved' }, { field: 'TotalAmount', operator: 'Greater Than', value: '10000' }], runCount: 15, lastRunOn: '2026-08-25', isShared: true },
    { id: '2', name: 'Unpaid Invoices 30+ days', entityName: 'Invoice', filters: [{ field: 'Status', operator: 'Equals', value: 'Open' }], runCount: 8, lastRunOn: '2026-08-24', isShared: false },
    { id: '3', name: 'Active Projects', entityName: 'Project', filters: [{ field: 'Status', operator: 'Equals', value: 'Active' }], runCount: 22, lastRunOn: '2026-08-27', isShared: true },
  ]);
  const [queryName, setQueryName] = useState('');
  const [isRunning, setIsRunning] = useState(false);
  const [activeTab, setActiveTab] = useState<'query' | 'results' | 'saved'>('query');

  const availableFields = selectedEntity ? (fieldOptions[selectedEntity] || ['Id', 'CreatedOn', 'ModifiedOn']) : [];

  const addFilter = () => {
    setFilters([...filters, { field: '', operator: 'Equals', value: '' }]);
  };

  const updateFilter = (index: number, updates: Partial<QueryFilter>) => {
    setFilters(filters.map((f, i) => (i === index ? { ...f, ...updates } : f)));
  };

  const removeFilter = (index: number) => {
    setFilters(filters.filter((_, i) => i !== index));
  };

  const runQuery = async () => {
    if (!selectedEntity) return;
    setIsRunning(true);
    setActiveTab('results');

    // Simulate query execution
    await new Promise((r) => setTimeout(r, 800));

    const mockResults: Record<string, unknown>[] = [];
    for (let i = 0; i < 12; i++) {
      const row: Record<string, unknown> = {};
      availableFields.forEach((f) => {
        if (f.includes('Amount') || f.includes('Cost') || f.includes('Quantity')) {
          row[f] = Math.round(Math.random() * 100000) / 100;
        } else if (f.includes('Date') || f.includes('On')) {
          row[f] = new Date(2026, Math.floor(Math.random() * 12), Math.floor(Math.random() * 28) + 1).toISOString().split('T')[0];
        } else if (f === 'Status') {
          row[f] = ['Active', 'Approved', 'Open', 'Pending'][Math.floor(Math.random() * 4)];
        } else {
          row[f] = `${f}_${i + 1}`;
        }
      });
      mockResults.push(row);
    }

    setResults(mockResults);
    setIsRunning(false);
  };

  const saveQuery = () => {
    if (!queryName.trim() || !selectedEntity) return;
    const newQuery: SavedQuery = {
      id: Date.now().toString(),
      name: queryName,
      entityName: selectedEntity,
      filters: [...filters],
      runCount: 0,
      lastRunOn: null,
      isShared: false,
    };
    setSavedQueries([newQuery, ...savedQueries]);
    setQueryName('');
  };

  const loadSavedQuery = (query: SavedQuery) => {
    setSelectedEntity(query.entityName);
    setFilters(query.filters);
    setActiveTab('query');
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Quick Query</h1>
          <p className="text-sm text-gray-500">Build ad-hoc queries across all modules, save and share with your team</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b">
        {(['query', 'results', 'saved'] as const).map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 text-sm font-medium border-b-2 ${
              activeTab === tab
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {tab === 'query' && <Search size={14} className="inline mr-1" />}
            {tab === 'results' && <Play size={14} className="inline mr-1" />}
            {tab === 'saved' && <Star size={14} className="inline mr-1" />}
            {tab.charAt(0).toUpperCase() + tab.slice(1)}
            {tab === 'results' && results.length > 0 && (
              <span className="ml-1 bg-blue-100 text-blue-700 text-xs rounded-full px-1.5">{results.length}</span>
            )}
            {tab === 'saved' && <span className="ml-1 bg-gray-100 text-gray-600 text-xs rounded-full px-1.5">{savedQueries.length}</span>}
          </button>
        ))}
      </div>

      {/* Query Builder Tab */}
      {activeTab === 'query' && (
        <div className="grid grid-cols-3 gap-6">
          <div className="col-span-2 space-y-4">
            <div className="bg-white rounded-lg border p-4">
              <h3 className="font-semibold text-sm mb-3 flex items-center gap-2">
                <Filter size={14} /> Entity & Filters
              </h3>
              <div className="mb-3">
                <label className="text-xs text-gray-600 mb-1 block">Data Source</label>
                <select
                  value={selectedEntity}
                  onChange={(e) => { setSelectedEntity(e.target.value); setFilters([]); setResults([]); }}
                  className="w-full border rounded-lg px-3 py-2 text-sm"
                >
                  <option value="">Select entity...</option>
                  {entityOptions.map((e) => (
                    <option key={e} value={e}>{e}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                {filters.map((filter, index) => (
                  <div key={index} className="flex gap-2 items-center">
                    <select
                      value={filter.field}
                      onChange={(e) => updateFilter(index, { field: e.target.value })}
                      className="flex-1 border rounded px-2 py-1.5 text-sm"
                    >
                      <option value="">Field...</option>
                      {availableFields.map((f) => (
                        <option key={f} value={f}>{f}</option>
                      ))}
                    </select>
                    <select
                      value={filter.operator}
                      onChange={(e) => updateFilter(index, { operator: e.target.value })}
                      className="w-36 border rounded px-2 py-1.5 text-sm"
                    >
                      {operatorOptions.map((op) => (
                        <option key={op} value={op}>{op}</option>
                      ))}
                    </select>
                    <input
                      value={filter.value}
                      onChange={(e) => updateFilter(index, { value: e.target.value })}
                      placeholder="Value"
                      className="flex-1 border rounded px-2 py-1.5 text-sm"
                    />
                    <button onClick={() => removeFilter(index)} className="p-1.5 hover:bg-red-50 rounded text-red-500">
                      <Trash2 size={14} />
                    </button>
                  </div>
                ))}
              </div>

              <div className="mt-3 flex gap-2">
                <button onClick={addFilter} className="text-sm text-blue-600 hover:text-blue-800 flex items-center gap-1">
                  <Filter size={12} /> Add Filter
                </button>
              </div>
            </div>

            <div className="flex gap-2">
              <button
                onClick={runQuery}
                disabled={!selectedEntity || isRunning}
                className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                <Play size={16} /> {isRunning ? 'Running...' : 'Run Query'}
              </button>
              <button
                onClick={() => { setFilters([]); setResults([]); }}
                className="px-4 py-2 bg-gray-200 rounded-lg hover:bg-gray-300"
              >
                Clear
              </button>
            </div>
          </div>

          {/* Column Selection */}
          <div className="bg-white rounded-lg border p-4">
            <h3 className="font-semibold text-sm mb-3 flex items-center gap-2">
              <Columns size={14} /> Output Columns
            </h3>
            {selectedEntity ? (
              <div className="space-y-1.5">
                {availableFields.map((field) => (
                  <label key={field} className="flex items-center gap-2 text-sm">
                    <input type="checkbox" defaultChecked className="rounded" />
                    {field}
                  </label>
                ))}
              </div>
            ) : (
              <p className="text-sm text-gray-400">Select a data source first</p>
            )}
          </div>
        </div>
      )}

      {/* Results Tab */}
      {activeTab === 'results' && (
        <div className="bg-white rounded-lg border">
          <div className="p-3 border-b flex items-center justify-between">
            <span className="text-sm text-gray-600">
              {results.length > 0 ? `${results.length} rows returned` : 'No results — run a query first'}
            </span>
            {results.length > 0 && (
              <button className="flex items-center gap-1 px-3 py-1 bg-green-600 text-white text-xs rounded hover:bg-green-700">
                <Download size={12} /> Export CSV
              </button>
            )}
          </div>
          {results.length > 0 && (
            <div className="overflow-x-auto max-h-[500px] overflow-y-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 sticky top-0">
                  <tr>
                    {Object.keys(results[0]).map((key) => (
                      <th key={key} className="px-3 py-2 text-left text-xs font-medium text-gray-600 border-b">
                        {key}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {results.map((row, i) => (
                    <tr key={i} className="hover:bg-gray-50">
                      {Object.values(row).map((val, j) => (
                        <td key={j} className="px-3 py-2 border-b text-sm">
                          {typeof val === 'number' ? val.toLocaleString() : String(val ?? '')}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Saved Queries Tab */}
      {activeTab === 'saved' && (
        <div className="space-y-4">
          <div className="bg-white rounded-lg border p-4">
            <h3 className="font-semibold text-sm mb-2">Save Current Query</h3>
            <div className="flex gap-2">
              <input
                value={queryName}
                onChange={(e) => setQueryName(e.target.value)}
                placeholder="Query name"
                className="flex-1 border rounded px-3 py-2 text-sm"
              />
              <button
                onClick={saveQuery}
                disabled={!queryName.trim() || !selectedEntity}
                className="flex items-center gap-1 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 text-sm"
              >
                <Save size={14} /> Save
              </button>
            </div>
          </div>

          <div className="bg-white rounded-lg border divide-y">
            {savedQueries.map((query) => (
              <div key={query.id} className="p-3 flex items-center justify-between hover:bg-gray-50">
                <div>
                  <div className="font-medium text-sm flex items-center gap-2">
                    {query.name}
                    {query.isShared && <span className="text-xs bg-blue-100 text-blue-700 px-1.5 rounded">Shared</span>}
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    {query.entityName} • {query.filters.length} filter{query.filters.length !== 1 ? 's' : ''}
                    {query.lastRunOn && <> • Last run: {query.lastRunOn}</>}
                    {query.runCount > 0 && <> • Run {query.runCount}x</>}
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => loadSavedQuery(query)}
                    className="px-3 py-1 bg-blue-100 text-blue-700 text-xs rounded hover:bg-blue-200"
                  >
                    Load
                  </button>
                  <button className="px-3 py-1 bg-gray-100 text-gray-600 text-xs rounded hover:bg-gray-200">
                    Share
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
