import { useState } from 'react';
import { ArrowRight, ExternalLink, Filter, Search } from 'lucide-react';

interface DrillBackEntry {
  id: string;
  sourceModule: string;
  sourceType: string;
  documentNumber: string;
  description: string;
  amount: number;
  date: string;
  status: string;
  accountNumber: string;
  period: string;
}

const modules = ['General Ledger', 'Accounts Payable', 'Accounts Receivable', 'Inventory', 'Purchasing', 'Order Management', 'Payroll', 'Project Accounting'];

const sampleData: DrillBackEntry[] = [
  { id: '1', sourceModule: 'Accounts Payable', sourceType: 'Voucher', documentNumber: 'VCH-2026-001', description: 'Office Supplies - Staples', amount: 1250.00, date: '2026-08-15', status: 'Posted', accountNumber: '6010', period: 'AUG-26' },
  { id: '2', sourceModule: 'Accounts Receivable', sourceType: 'Invoice', documentNumber: 'INV-2026-0142', description: 'Project Alpha - July Progress Billing', amount: 45000.00, date: '2026-08-01', status: 'Posted', accountNumber: '4000', period: 'AUG-26' },
  { id: '3', sourceModule: 'Inventory', sourceType: 'Receipt', documentNumber: 'REC-2026-0089', description: 'Steel Beam Delivery - PO-2026-0045', amount: 12500.00, date: '2026-08-12', status: 'Posted', accountNumber: '1200', period: 'AUG-26' },
  { id: '4', sourceModule: 'Purchasing', sourceType: 'Purchase Order', documentNumber: 'PO-2026-0067', description: 'Concrete Mix - ABC Supply', amount: 8750.00, date: '2026-08-10', status: 'Approved', accountNumber: '5000', period: 'AUG-26' },
  { id: '5', sourceModule: 'Order Management', sourceType: 'Sales Order', documentNumber: 'SO-2026-0234', description: 'Widget Order - Customer XYZ', amount: 23400.00, date: '2026-08-18', status: 'Confirmed', accountNumber: '4000', period: 'AUG-26' },
  { id: '6', sourceModule: 'Payroll', sourceType: 'Payroll Run', documentNumber: 'PR-2026-017', description: 'Bi-weekly Payroll - Period 17', amount: 125000.00, date: '2026-08-22', status: 'Posted', accountNumber: '6100', period: 'AUG-26' },
  { id: '7', sourceModule: 'Project Accounting', sourceType: 'Cost Transaction', documentNumber: 'CST-2026-0456', description: 'Labor - Project Beta', amount: 5600.00, date: '2026-08-19', status: 'Posted', accountNumber: '6100', period: 'AUG-26' },
  { id: '8', sourceModule: 'General Ledger', sourceType: 'Journal Batch', documentNumber: 'JB-2026-0189', description: 'Month-end Depreciation Entry', amount: 15000.00, date: '2026-08-31', status: 'Draft', accountNumber: '1510', period: 'AUG-26' },
];

const moduleRoutes: Record<string, string> = {
  'General Ledger': '/gl/journal-batches',
  'Accounts Payable': '/ap/vouchers',
  'Accounts Receivable': '/ar/invoices',
  'Inventory': '/inventory/transactions',
  'Purchasing': '/purchasing/purchase-orders',
  'Order Management': '/om/sales-orders',
  'Payroll': '/payroll/runs',
  'Project Accounting': '/projects/costs',
};

export default function DrillBackPage() {
  const [entries] = useState<DrillBackEntry[]>(sampleData);
  const [moduleFilter, setModuleFilter] = useState('');
  const [search, setSearch] = useState('');
  const [selectedEntry, setSelectedEntry] = useState<DrillBackEntry | null>(null);

  const filtered = entries.filter((e) => {
    if (moduleFilter && e.sourceModule !== moduleFilter) return false;
    if (search) {
      const s = search.toLowerCase();
      return e.documentNumber.toLowerCase().includes(s) ||
        e.description.toLowerCase().includes(s) ||
        e.accountNumber.includes(s);
    }
    return true;
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Drill-Back Viewer</h1>
          <p className="text-sm text-gray-500">Navigate from summary reports to source transactions across all modules</p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-3">
        <div className="relative flex-1 max-w-md">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by document #, description, or account..."
            className="w-full pl-9 pr-3 py-2 border rounded-lg text-sm"
          />
        </div>
        <select
          value={moduleFilter}
          onChange={(e) => setModuleFilter(e.target.value)}
          className="border rounded-lg px-3 py-2 text-sm"
        >
          <option value="">All Modules</option>
          {modules.map((m) => (
            <option key={m} value={m}>{m}</option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Transaction List */}
        <div className="col-span-2 bg-white rounded-lg border">
          <div className="p-3 border-b font-semibold text-sm text-gray-600">
            Transactions ({filtered.length})
          </div>
          <div className="divide-y max-h-[600px] overflow-y-auto">
            {filtered.map((entry) => (
              <div
                key={entry.id}
                className={`p-3 cursor-pointer hover:bg-gray-50 ${selectedEntry?.id === entry.id ? 'bg-blue-50' : ''}`}
                onClick={() => setSelectedEntry(entry)}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center text-xs font-bold text-gray-600">
                      {entry.sourceModule.substring(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <div className="font-medium text-sm">{entry.documentNumber}</div>
                      <div className="text-xs text-gray-500">{entry.description}</div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-medium text-sm">${entry.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
                    <div className="text-xs text-gray-500">{entry.date}</div>
                  </div>
                </div>
                <div className="flex gap-2 mt-2">
                  <span className="text-xs bg-gray-100 px-2 py-0.5 rounded">{entry.sourceModule}</span>
                  <span className="text-xs bg-gray-100 px-2 py-0.5 rounded">Acct {entry.accountNumber}</span>
                  <span className={`text-xs px-2 py-0.5 rounded ${
                    entry.status === 'Posted' ? 'bg-green-100 text-green-700' :
                    entry.status === 'Draft' ? 'bg-yellow-100 text-yellow-700' :
                    'bg-blue-100 text-blue-700'
                  }`}>
                    {entry.status}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Detail Panel */}
        <div className="bg-white rounded-lg border">
          {selectedEntry ? (
            <div className="p-4 space-y-4">
              <h3 className="font-semibold">{selectedEntry.documentNumber}</h3>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-500">Module</span>
                  <span>{selectedEntry.sourceModule}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Type</span>
                  <span>{selectedEntry.sourceType}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Description</span>
                  <span className="text-right max-w-[200px]">{selectedEntry.description}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Amount</span>
                  <span className="font-bold">${selectedEntry.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Date</span>
                  <span>{selectedEntry.date}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Period</span>
                  <span>{selectedEntry.period}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">GL Account</span>
                  <span>{selectedEntry.accountNumber}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Status</span>
                  <span className={`px-2 py-0.5 rounded text-xs ${
                    selectedEntry.status === 'Posted' ? 'bg-green-100 text-green-700' :
                    selectedEntry.status === 'Draft' ? 'bg-yellow-100 text-yellow-700' :
                    'bg-blue-100 text-blue-700'
                  }`}>
                    {selectedEntry.status}
                  </span>
                </div>
              </div>

              <div className="border-t pt-3 space-y-2">
                <h4 className="text-xs font-semibold text-gray-600 uppercase">Navigation</h4>
                <a
                  href={moduleRoutes[selectedEntry.sourceModule] || '#'}
                  className="flex items-center gap-2 px-3 py-2 bg-blue-50 rounded-lg text-sm text-blue-700 hover:bg-blue-100"
                >
                  <ExternalLink size={14} />
                  View in {selectedEntry.sourceModule}
                  <ArrowRight size={14} className="ml-auto" />
                </a>
                <button className="flex items-center gap-2 px-3 py-2 bg-gray-50 rounded-lg text-sm text-gray-700 hover:bg-gray-100 w-full">
                  <Filter size={14} />
                  View Related Transactions
                </button>
                <button className="flex items-center gap-2 px-3 py-2 bg-gray-50 rounded-lg text-sm text-gray-700 hover:bg-gray-100 w-full">
                  <Search size={14} />
                  View Audit Trail
                </button>
              </div>
            </div>
          ) : (
            <div className="p-12 text-center text-gray-400">
              <ArrowRight size={48} className="mx-auto mb-4" />
              <p className="text-sm">Select a transaction to view details and drill back to the source document</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
