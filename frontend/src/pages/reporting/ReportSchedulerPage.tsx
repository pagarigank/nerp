import { useState } from 'react';
import { Clock, Plus, Play, Pause, Trash2, Mail, FileText, Calendar, Settings } from 'lucide-react';

interface ReportSubscription {
  id: string;
  name: string;
  reportName: string;
  reportModule: string;
  exportFormat: string;
  scheduleType: string;
  scheduleConfig: string;
  recipients: string[];
  lastRunOn: string | null;
  lastRunStatus: string | null;
  runCount: number;
  isActive: boolean;
}

const availableReports = [
  { name: 'AP Aging', module: 'Accounts Payable' },
  { name: 'AR Aging', module: 'Accounts Receivable' },
  { name: 'Trial Balance', module: 'General Ledger' },
  { name: 'Income Statement', module: 'General Ledger' },
  { name: 'Balance Sheet', module: 'General Ledger' },
  { name: 'Cash Position', module: 'Cash Management' },
  { name: 'Open PO Report', module: 'Purchasing' },
  { name: 'Inventory Valuation', module: 'Inventory' },
  { name: 'Sales Analysis', module: 'Order Management' },
  { name: 'Project WIP Schedule', module: 'Project Accounting' },
  { name: 'Payroll Register', module: 'Payroll' },
  { name: 'Budget vs Actual', module: 'General Ledger' },
];

export default function ReportSchedulerPage() {
  const [subscriptions, setSubscriptions] = useState<ReportSubscription[]>([
    {
      id: '1', name: 'Weekly AP Aging', reportName: 'AP Aging', reportModule: 'Accounts Payable',
      exportFormat: 'PDF', scheduleType: 'Weekly', scheduleConfig: 'Every Monday at 8:00 AM',
      recipients: ['cfo@company.com', 'ap-manager@company.com'],
      lastRunOn: '2026-08-25', lastRunStatus: 'Success', runCount: 12, isActive: true,
    },
    {
      id: '2', name: 'Monthly Financial Package', reportName: 'Income Statement', reportModule: 'General Ledger',
      exportFormat: 'Excel', scheduleType: 'Monthly', scheduleConfig: '1st of each month at 6:00 AM',
      recipients: ['cfo@company.com', 'controller@company.com', 'board@company.com'],
      lastRunOn: '2026-08-01', lastRunStatus: 'Success', runCount: 8, isActive: true,
    },
    {
      id: '3', name: 'Daily Cash Position', reportName: 'Cash Position', reportModule: 'Cash Management',
      exportFormat: 'CSV', scheduleType: 'Daily', scheduleConfig: 'Every day at 7:00 AM',
      recipients: ['treasury@company.com'],
      lastRunOn: '2026-08-27', lastRunStatus: 'Success', runCount: 45, isActive: true,
    },
    {
      id: '4', name: 'Quarterly Inventory Valuation', reportName: 'Inventory Valuation', reportModule: 'Inventory',
      exportFormat: 'PDF', scheduleType: 'Monthly', scheduleConfig: 'Last day of month',
      recipients: ['warehouse-manager@company.com', 'cfo@company.com'],
      lastRunOn: '2026-06-30', lastRunStatus: 'Failed', runCount: 3, isActive: false,
    },
  ]);

  const [showNew, setShowNew] = useState(false);
  const [newSub, setNewSub] = useState({
    name: '',
    reportName: '',
    reportModule: '',
    exportFormat: 'PDF',
    scheduleType: 'Weekly',
    recipients: '',
  });

  const handleCreate = () => {
    if (!newSub.name || !newSub.reportName) return;
    const sub: ReportSubscription = {
      id: Date.now().toString(),
      name: newSub.name,
      reportName: newSub.reportName,
      reportModule: newSub.reportModule,
      exportFormat: newSub.exportFormat,
      scheduleType: newSub.scheduleType,
      scheduleConfig: '',
      recipients: newSub.recipients.split(',').map((r) => r.trim()).filter(Boolean),
      lastRunOn: null,
      lastRunStatus: null,
      runCount: 0,
      isActive: true,
    };
    setSubscriptions([sub, ...subscriptions]);
    setShowNew(false);
    setNewSub({ name: '', reportName: '', reportModule: '', exportFormat: 'PDF', scheduleType: 'Weekly', recipients: '' });
  };

  const toggleActive = (id: string) => {
    setSubscriptions(subscriptions.map((s) =>
      s.id === id ? { ...s, isActive: !s.isActive } : s
    ));
  };

  const deleteSub = (id: string) => {
    setSubscriptions(subscriptions.filter((s) => s.id !== id));
  };

  const runNow = (id: string) => {
    setSubscriptions(subscriptions.map((s) =>
      s.id === id ? { ...s, lastRunOn: new Date().toISOString().split('T')[0], lastRunStatus: 'Success', runCount: s.runCount + 1 } : s
    ));
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Report Scheduler</h1>
          <p className="text-sm text-gray-500">Schedule automated report delivery to stakeholders</p>
        </div>
        <button
          onClick={() => setShowNew(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          <Plus size={16} /> New Subscription
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Active Subscriptions', value: subscriptions.filter((s) => s.isActive).length, color: 'green' },
          { label: 'Total Runs', value: subscriptions.reduce((sum, s) => sum + s.runCount, 0), color: 'blue' },
          { label: 'Failed (Last Run)', value: subscriptions.filter((s) => s.lastRunStatus === 'Failed').length, color: 'red' },
          { label: 'Unique Recipients', value: new Set(subscriptions.flatMap((s) => s.recipients)).size, color: 'purple' },
        ].map((stat) => (
          <div key={stat.label} className="bg-white rounded-lg border p-4">
            <div className="text-2xl font-bold">{stat.value}</div>
            <div className="text-xs text-gray-500 mt-1">{stat.label}</div>
          </div>
        ))}
      </div>

      {/* New Subscription Form */}
      {showNew && (
        <div className="bg-white rounded-lg border p-4 space-y-3">
          <h3 className="font-semibold">New Subscription</h3>
          <div className="grid grid-cols-2 gap-3">
            <input
              placeholder="Subscription name"
              value={newSub.name}
              onChange={(e) => setNewSub({ ...newSub, name: e.target.value })}
              className="border rounded px-3 py-2 text-sm"
            />
            <select
              value={newSub.reportName}
              onChange={(e) => {
                const report = availableReports.find((r) => r.name === e.target.value);
                setNewSub({ ...newSub, reportName: e.target.value, reportModule: report?.module || '' });
              }}
              className="border rounded px-3 py-2 text-sm"
            >
              <option value="">Select report...</option>
              {availableReports.map((r) => (
                <option key={r.name} value={r.name}>{r.name} ({r.module})</option>
              ))}
            </select>
            <select
              value={newSub.exportFormat}
              onChange={(e) => setNewSub({ ...newSub, exportFormat: e.target.value })}
              className="border rounded px-3 py-2 text-sm"
            >
              <option value="PDF">PDF</option>
              <option value="Excel">Excel</option>
              <option value="CSV">CSV</option>
            </select>
            <select
              value={newSub.scheduleType}
              onChange={(e) => setNewSub({ ...newSub, scheduleType: e.target.value })}
              className="border rounded px-3 py-2 text-sm"
            >
              <option value="Daily">Daily</option>
              <option value="Weekly">Weekly</option>
              <option value="Monthly">Monthly</option>
              <option value="OnDemand">On Demand</option>
            </select>
            <input
              placeholder="Recipients (comma-separated emails)"
              value={newSub.recipients}
              onChange={(e) => setNewSub({ ...newSub, recipients: e.target.value })}
              className="col-span-2 border rounded px-3 py-2 text-sm"
            />
          </div>
          <div className="flex gap-2">
            <button onClick={handleCreate} className="px-4 py-2 bg-green-600 text-white text-sm rounded hover:bg-green-700">Create</button>
            <button onClick={() => setShowNew(false)} className="px-4 py-2 bg-gray-200 text-sm rounded hover:bg-gray-300">Cancel</button>
          </div>
        </div>
      )}

      {/* Subscriptions Table */}
      <div className="bg-white rounded-lg border">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Name</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Report</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Format</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Schedule</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Recipients</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Last Run</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Runs</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Status</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {subscriptions.map((sub) => (
                <tr key={sub.id} className={`hover:bg-gray-50 ${!sub.isActive ? 'opacity-50' : ''}`}>
                  <td className="px-4 py-3">
                    <div className="font-medium flex items-center gap-2">
                      <Clock size={14} className="text-gray-400" />
                      {sub.name}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div>{sub.reportName}</div>
                    <div className="text-xs text-gray-500">{sub.reportModule}</div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-0.5 bg-gray-100 rounded text-xs">{sub.exportFormat}</span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      <Calendar size={12} className="text-gray-400" />
                      <span className="text-xs">{sub.scheduleType}</span>
                    </div>
                    <div className="text-xs text-gray-500">{sub.scheduleConfig}</div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      <Mail size={12} className="text-gray-400" />
                      <span className="text-xs">{sub.recipients.length} recipient{sub.recipients.length !== 1 ? 's' : ''}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-xs">
                    {sub.lastRunOn ? (
                      <div>
                        <div>{sub.lastRunOn}</div>
                        <div className={sub.lastRunStatus === 'Success' ? 'text-green-600' : 'text-red-600'}>
                          {sub.lastRunStatus}
                        </div>
                      </div>
                    ) : (
                      <span className="text-gray-400">Never</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-xs">{sub.runCount}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs ${sub.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {sub.isActive ? 'Active' : 'Paused'}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1">
                      <button
                        onClick={() => runNow(sub.id)}
                        className="p-1.5 hover:bg-blue-50 rounded text-blue-600"
                        title="Run Now"
                      >
                        <Play size={14} />
                      </button>
                      <button
                        onClick={() => toggleActive(sub.id)}
                        className={`p-1.5 rounded ${sub.isActive ? 'hover:bg-yellow-50 text-yellow-600' : 'hover:bg-green-50 text-green-600'}`}
                        title={sub.isActive ? 'Pause' : 'Resume'}
                      >
                        <Pause size={14} />
                      </button>
                      <button
                        onClick={() => deleteSub(sub.id)}
                        className="p-1.5 hover:bg-red-50 rounded text-red-500"
                        title="Delete"
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
