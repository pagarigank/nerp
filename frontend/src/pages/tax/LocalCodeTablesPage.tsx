import { useState } from 'react'
import { Search, Table, Copy, Check } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'

// Local code tables maintained by LHDN for MyInvois compliance
const CODE_TABLES: Record<string, Array<{ code: string; description: string; category?: string }>> = {
  'Tax Types (LHDN)': [
    { code: '01', description: 'Sales Tax (Manufacturing)', category: 'Sales Tax' },
    { code: '02', description: 'Sales Tax (Import)', category: 'Sales Tax' },
    { code: '03', description: 'Sales Tax (Services)', category: 'Sales Tax' },
    { code: '04', description: 'Service Tax', category: 'Service Tax' },
    { code: 'E', description: 'Exempt', category: 'Exempt' },
    { code: 'ZRL', description: 'Zero Rated', category: 'Zero Rate' },
    { code: 'DS', description: 'Deemed Supply', category: 'Special' },
  ],
  'Classification Codes': [
    { code: '022', description: 'Goods' },
    { code: '031', description: 'Services' },
    { code: '032', description: 'Construction services' },
    { code: '033', description: 'Utility services' },
    { code: '034', description: 'Telecommunications services' },
    { code: '035', description: 'Transportation services' },
    { code: '036', description: 'Real estate services' },
    { code: '037', description: 'Professional services' },
    { code: '038', description: 'Government services' },
    { code: '039', description: 'Other services' },
  ],
  'Unit of Measure (UN/ECE)': [
    { code: 'C62', description: 'Each (unit)' },
    { code: 'KGM', description: 'Kilogram' },
    { code: 'GRM', description: 'Gram' },
    { code: 'LTR', description: 'Litre' },
    { code: 'MLT', description: 'Millilitre' },
    { code: 'MTR', description: 'Metre' },
    { code: 'MTK', description: 'Square metre' },
    { code: 'MTQ', description: 'Cubic metre' },
    { code: 'HUR', description: 'Hour' },
    { code: 'DAY', description: 'Day' },
    { code: 'MON', description: 'Month' },
    { code: 'SET', description: 'Set' },
    { code: 'BOX', description: 'Box' },
    { code: 'PAK', description: 'Package' },
    { code: 'BAG', description: 'Bag' },
    { code: 'NPR', description: 'Pair' },
    { code: 'RO', description: 'Roll' },
    { code: 'DZN', description: 'Dozen' },
    { code: 'BDL', description: 'Bundle' },
    { code: 'CT', description: 'Carton' },
  ],
  'ID Types (MyInvois)': [
    { code: 'NRIC', description: 'New IC / MyKad', category: 'Individual' },
    { code: 'PASS', description: 'Passport', category: 'Individual' },
    { code: 'ARMY', description: 'Army', category: 'Individual' },
    { code: 'BRN', description: 'Business Registration Number', category: 'Business' },
    { code: 'SSM', description: 'SSM Registration No.', category: 'Business' },
    { code: 'OLD_IC', description: 'Old IC', category: 'Individual' },
    { code: 'TRL', description: 'Tax Reference Number', category: 'Tax' },
  ],
  'Payment Modes (MyInvois)': [
    { code: '01', description: 'Cash' },
    { code: '02', description: 'Cheque' },
    { code: '03', description: 'Credit Card' },
    { code: '04', description: 'Bank Transfer' },
    { code: '05', description: 'QR Code' },
    { code: '06', description: 'Online Payment' },
    { code: '07', description: 'Debit Card' },
    { code: '08', description: 'E-Wallet' },
    { code: '09', description: 'Others' },
  ],
  'MSIC Codes (Selected)': [
    { code: '01111', description: 'Growing of rice', category: 'Agriculture' },
    { code: '01119', description: 'Growing of other non-perennial crops', category: 'Agriculture' },
    { code: '10100', description: 'Processing and preserving of meat', category: 'Manufacturing' },
    { code: '10200', description: 'Processing and preserving of fish', category: 'Manufacturing' },
    { code: '10799', description: 'Manufacture of other food products n.e.c.', category: 'Manufacturing' },
    { code: '11010', description: 'Distilling, rectifying and blending of spirits', category: 'Manufacturing' },
    { code: '25110', description: 'Manufacture of metal structures and parts', category: 'Manufacturing' },
    { code: '26800', description: 'Manufacture of magnetic and optical media', category: 'Manufacturing' },
    { code: '41000', description: 'Construction of buildings', category: 'Construction' },
    { code: '46900', description: 'Wholesale trade not in stores or markets', category: 'Wholesale' },
    { code: '47110', description: 'Retail sale in non-specialised stores', category: 'Retail' },
    { code: '62010', description: 'Computer programming activities', category: 'ICT' },
    { code: '62020', description: 'Computer consultancy activities', category: 'ICT' },
    { code: '62090', description: 'Other information technology services', category: 'ICT' },
    { code: '70200', description: 'Management consultancy activities', category: 'Professional' },
    { code: '73110', description: 'Advertising agencies', category: 'Professional' },
    { code: '82110', description: 'Combined office administrative service activities', category: 'Admin' },
    { code: '82990', description: 'Other business support service activities n.e.c.', category: 'Admin' },
    { code: '85110', description: 'Pre-primary education', category: 'Education' },
    { code: '86100', description: 'Hospital activities', category: 'Health' },
    { code: '86200', description: 'Medical and dental practice activities', category: 'Health' },
  ],
}

function copyToClipboard(text: string) {
  navigator.clipboard.writeText(text)
}

export function LocalCodeTablesPage() {
  const [search, setSearch] = useState('')
  const [activeTable, setActiveTable] = useState(Object.keys(CODE_TABLES)[0])
  const [copiedCode, setCopiedCode] = useState<string | null>(null)

  const handleCopy = (code: string) => {
    copyToClipboard(code)
    setCopiedCode(code)
    setTimeout(() => setCopiedCode(null), 1500)
  }

  const codes = CODE_TABLES[activeTable] || []
  const filtered = codes.filter(c => {
    const q = search.trim().toLowerCase()
    if (!q) return true
    return c.code.toLowerCase().includes(q) ||
      c.description.toLowerCase().includes(q) ||
      c.category?.toLowerCase().includes(q)
  })

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="Local Code Tables"
          description="Reference data maintained by LHDN for MyInvois e-Invoice compliance. These codes are used in UBL 2.1 documents."
        />
        <CardContent>
          {/* Table Tabs */}
          <div className="flex flex-wrap gap-2 mb-4">
            {Object.keys(CODE_TABLES).map(tableName => (
              <button
                key={tableName}
                onClick={() => { setActiveTable(tableName); setSearch('') }}
                className={`px-3 py-1.5 text-xs font-medium rounded-lg transition-colors ${
                  activeTable === tableName
                    ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-400 dark:hover:bg-gray-700'
                }`}
              >
                {tableName}
                <span className="ml-1.5 text-[10px] opacity-60">({CODE_TABLES[tableName].length})</span>
              </button>
            ))}
          </div>

          <div className="mb-4 max-w-md">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search codes or descriptions..."
              leftIcon={<Search className="h-4 w-4" />} aria-label="Search code tables" />
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                  {codes.some(c => c.category) && (
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Category</th>
                  )}
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right w-16">Copy</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {filtered.map(c => (
                  <tr key={c.code} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                    <td className="px-3 py-2">
                      <code className="text-xs font-mono bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded">{c.code}</code>
                    </td>
                    <td className="px-3 py-2 text-gray-700 dark:text-gray-300">{c.description}</td>
                    {codes.some(code => code.category) && (
                      <td className="px-3 py-2">
                        {c.category ? <Badge variant="info" size="sm">{c.category}</Badge> : <span className="text-gray-400">—</span>}
                      </td>
                    )}
                    <td className="px-3 py-2 text-right">
                      <button onClick={() => handleCopy(c.code)} className="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                        title="Copy code to clipboard">
                        {copiedCode === c.code ? (
                          <Check className="h-3.5 w-3.5 text-emerald-500" />
                        ) : (
                          <Copy className="h-3.5 w-3.5 text-gray-400" />
                        )}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 text-xs text-gray-500 dark:text-gray-400">
            Showing {filtered.length} of {codes.length} codes
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
