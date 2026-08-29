#!/usr/bin/env node
// Reads e2e-results.jsonl and writes D:/nerp/E2E_SUMMARY.md
import { readFileSync, existsSync, writeFileSync } from 'fs'

const FILE = 'e2e-results.jsonl'
const OUT = 'D:/nerp/E2E_SUMMARY.md'

if (!existsSync(FILE)) {
  console.error('No e2e-results.jsonl found. Run `npx playwright test` first.')
  process.exit(1)
}

const lines = readFileSync(FILE, 'utf8').trim().split('\n').filter(Boolean)
const rows = lines.map(l => JSON.parse(l))

const order = ['SUCCESS', 'SUCCESS_NO_CONFIRM', 'READ_ONLY_OK', 'VALIDATION', 'SERVER_ERROR', 'NETWORK_ERROR', 'NO_FORM', 'BLOCKED']
const counts = {}
for (const r of rows) counts[r.status] = (counts[r.status] || 0) + 1

const byStatus = (s) => rows.filter(r => r.status === s)

let md = `# E2E Form-Based Testing — Summary\n\n`
md += `Generated: ${new Date().toISOString()}\n`
md += `Total module flows exercised: **${rows.length}**\n\n`
md += `## Outcome tally\n\n`
md += `| Outcome | Count |\n|---|---|\n`
for (const s of order) if (counts[s]) md += `| ${s} | ${counts[s]} |\n`
md += `\n`

md += `## Per-module results\n\n`
md += `| Module | Path | Status | Filled | Detail |\n|---|---|---|---|---|\n`
for (const r of rows) {
  const filled = r.filled !== undefined ? r.filled : '-'
  md += `| ${r.module} | ${r.path} | \`${r.status}\` | ${filled} | ${String(r.detail || '').replace(/\|/g, '\\\\|').slice(0, 160)} |\n`
}

md += `\n## Issues found (frontend → backend)\n\n`
const issues = byStatus('SERVER_ERROR').concat(byStatus('NETWORK_ERROR')).concat(byStatus('VALIDATION')).concat(byStatus('NO_FORM')).concat(byStatus('BLOCKED'))
if (!issues.length) {
  md += `No failures recorded. All module create-form flows either succeeded or are read-only pages.\n`
} else {
  for (const r of issues) {
    md += `### ${r.module} — ${r.status}\n`
    md += `- Path: \`${r.path}\`\n`
    md += `- Detail: ${String(r.detail || '').replace(/\|/g, '\\\\|')}\n`
    if (r.skipped && r.skipped.length) md += `- Skipped fields: ${r.skipped.join(', ')}\n`
    md += `\n`
  }
}

md += `\n## Raw results\n\n`
md += '```json\n' + JSON.stringify(rows, null, 2) + '\n```\n'

writeFileSync(OUT, md)
console.log(`Wrote ${OUT} (${rows.length} rows)`)
