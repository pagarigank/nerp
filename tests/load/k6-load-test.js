/**
 * k6 Load Test Script — ERP System
 *
 * Targets:
 * - 500 concurrent users
 * - Realistic transaction mix (reads:writes = 80:20)
 * - p95 response time < 2 seconds
 * - Error rate < 0.1%
 * - Throughput > 500 TPS
 *
 * Run:
 *   k6 run --vus 500 --duration 1h tests/load/k6-load-test.js
 *
 * Ramp-up:
 *   k6 run --vus 10 --stages "duration:2m,target:50" tests/load/k6-load-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';

// ─── Custom Metrics ──────────────────────────────────────────────────────────
const apiErrors = new Counter('api_errors');
const apiSuccess = new Counter('api_success');
const errorRate = new Rate('error_rate');
const loginDuration = new Trend('login_duration');
const readDuration = new Trend('read_duration');
const writeDuration = new Trend('write_duration');
const activeVUs = new Gauge('active_vus');

// ─── Configuration ───────────────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const COMPANY_ID = __ENV.COMPANY_ID || '00000000-0000-0000-0000-000000000001';

// Test users — rotate through these for realistic auth mix
const TEST_USERS = [
  { username: 'admin', password: 'P@ssw0rd123!' },
  { username: 'accountant', password: 'P@ssw0rd123!' },
  { username: 'ap-clerk', password: 'P@ssw0rd123!' },
  { username: 'ar-clerk', password: 'P@ssw0rd123!' },
  { username: 'purchaser', password: 'P@ssw0rd123!' },
];

// ─── Test Options ────────────────────────────────────────────────────────────
export const options = {
  stages: [
    { duration: '2m', target: 50 },    // Ramp up to 50 users
    { duration: '5m', target: 100 },   // Ramp to 100
    { duration: '5m', target: 200 },   // Ramp to 200
    { duration: '5m', target: 500 },   // Ramp to 500
    { duration: '30m', target: 500 },  // Sustain 500 for 30 minutes
    { duration: '5m', target: 0 },     // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],  // 95th percentile < 2s
    error_rate: ['rate<0.001'],         // Error rate < 0.1%
    http_reqs: ['rate>500'],            // Throughput > 500 TPS
  },
};

// ─── Setup: Authenticate ─────────────────────────────────────────────────────
export function setup() {
  const user = TEST_USERS[0];
  const loginRes = http.post(
    `${BASE_URL}/api/v1/platform/auth/login`,
    JSON.stringify({ username: user.username, password: user.password }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  check(loginRes, {
    'login successful': (r) => r.status === 200,
  });

  if (loginRes.status === 200) {
    const body = JSON.parse(loginRes.body);
    return { token: body.data?.token || body.token };
  }

  return { token: null };
}

// ─── Main Test Logic ─────────────────────────────────────────────────────────
export default function (data) {
  activeVUs.add(__VU);

  if (!data.token) {
    apiErrors.add(1);
    return;
  }

  const headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${data.token}`,
  };

  // ─── Read Operations (80% of traffic) ──────────────────────────────────────
  if (Math.random() < 0.8) {
    group('Read Operations', () => {
      const readOp = Math.random();

      if (readOp < 0.15) {
        // Dashboard data
        readApi(`/api/v1/platform/companies`, headers, 'list companies');
      } else if (readOp < 0.25) {
        // GL accounts
        readApi(`/api/v1/gl/accounts`, headers, 'list GL accounts');
      } else if (readOp < 0.35) {
        // AP vendors
        readApi(`/api/v1/ap/vendors`, headers, 'list vendors');
      } else if (readOp < 0.45) {
        // AR customers
        readApi(`/api/v1/ar/customers`, headers, 'list customers');
      } else if (readOp < 0.55) {
        // Inventory items
        readApi(`/api/v1/inventory/items`, headers, 'list inventory items');
      } else if (readOp < 0.65) {
        // Journal batches
        readApi(`/api/v1/gl/journal-batches`, headers, 'list journal batches');
      } else if (readOp < 0.72) {
        // Bank accounts
        readApi(`/api/v1/cash/bank-accounts`, headers, 'list bank accounts');
      } else if (readOp < 0.78) {
        // Purchase orders
        readApi(`/api/v1/pur/purchase-orders`, headers, 'list purchase orders');
      } else if (readOp < 0.84) {
        // Employees
        readApi(`/api/v1/payroll/employees`, headers, 'list employees');
      } else if (readOp < 0.90) {
        // Fiscal periods
        readApi(`/api/v1/platform/fiscal-periods`, headers, 'list fiscal periods');
      } else if (readOp < 0.95) {
        // Audit logs
        readApi(`/api/v1/platform/audit-logs`, headers, 'list audit logs');
      } else {
        // Performance metrics
        readApi(`/metrics`, headers, 'get metrics');
      }
    });
  }

  // ─── Write Operations (20% of traffic) ─────────────────────────────────────
  else {
    group('Write Operations', () => {
      const writeOp = Math.random();

      if (writeOp < 0.3) {
        // Create a journal batch
        createJournalBatch(headers);
      } else if (writeOp < 0.5) {
        // Create an AP voucher
        createApVoucher(headers);
      } else if (writeOp < 0.65) {
        // Update a company setting
        updateCompanySetting(headers);
      } else if (writeOp < 0.8) {
        // Create a requisition
        createRequisition(headers);
      } else {
        // Create an inventory transaction
        createInventoryTransaction(headers);
      }
    });
  }

  // Think time (simulate user pauses)
  sleep(Math.random() * 2 + 0.5); // 0.5s - 2.5s
}

// ─── Helper Functions ────────────────────────────────────────────────────────
function readApi(path, headers, name) {
  const res = http.get(`${BASE_URL}${path}`, { headers, tags: { operation: name } });
  const success = check(res, {
    [`${name} status 200`]: (r) => r.status === 200,
    [`${name} response time < 2s`]: (r) => r.timings.duration < 2000,
  });

  if (success) {
    apiSuccess.add(1);
  } else {
    apiErrors.add(1);
  }
  errorRate.add(!success);
  readDuration.add(res.timings.duration);
}

function createJournalBatch(headers) {
  const payload = {
    companyId: COMPANY_ID,
    description: `Load Test Batch ${Date.now()}`,
    batchDate: new Date().toISOString(),
    lines: [
      {
        accountId: '00000000-0000-0000-0000-000000000001',
        debitAmount: Math.random() * 1000,
        creditAmount: 0,
        description: 'Load test line',
      },
      {
        accountId: '00000000-0000-0000-0000-000000000002',
        debitAmount: 0,
        creditAmount: Math.random() * 1000,
        description: 'Load test line',
      },
    ],
  };

  const res = http.post(
    `${BASE_URL}/api/v1/gl/journal-batches`,
    JSON.stringify(payload),
    { headers, tags: { operation: 'create journal batch' } },
  );

  const success = check(res, {
    'create journal batch status 201 or 200': (r) => r.status === 200 || r.status === 201,
  });
  apiSuccess.add(success ? 1 : 0);
  apiErrors.add(success ? 0 : 1);
  errorRate.add(!success);
  writeDuration.add(res.timings.duration);
}

function createApVoucher(headers) {
  const payload = {
    companyId: COMPANY_ID,
    vendorId: '00000000-0000-0000-0000-000000000001',
    voucherDate: new Date().toISOString(),
    description: `Load Test Voucher ${Date.now()}`,
    totalAmount: Math.random() * 5000,
    lines: [
      {
        accountId: '00000000-0000-0000-0000-000000000001',
        amount: Math.random() * 5000,
        description: 'Load test expense',
      },
    ],
  };

  const res = http.post(
    `${BASE_URL}/api/v1/ap/vouchers`,
    JSON.stringify(payload),
    { headers, tags: { operation: 'create AP voucher' } },
  );

  const success = check(res, {
    'create AP voucher status 201 or 200': (r) => r.status === 200 || r.status === 201,
  });
  apiSuccess.add(success ? 1 : 0);
  apiErrors.add(success ? 0 : 1);
  errorRate.add(!success);
  writeDuration.add(res.timings.duration);
}

function updateCompanySetting(headers) {
  const res = http.patch(
    `${BASE_URL}/api/v1/platform/companies/${COMPANY_ID}`,
    JSON.stringify({}),
    { headers, tags: { operation: 'update company' } },
  );

  // Expect 200 or 400 (validation) — 500 is a real error
  const success = check(res, {
    'update company not 500': (r) => r.status !== 500,
  });
  apiSuccess.add(success ? 1 : 0);
  apiErrors.add(success ? 0 : 1);
  errorRate.add(!success);
  writeDuration.add(res.timings.duration);
}

function createRequisition(headers) {
  const payload = {
    companyId: COMPANY_ID,
    requestDate: new Date().toISOString(),
    description: `Load Test Requisition ${Date.now()}`,
    lines: [
      {
        itemId: '00000000-0000-0000-0000-000000000001',
        quantity: Math.ceil(Math.random() * 10),
        unitCost: Math.random() * 100,
        description: 'Load test item',
      },
    ],
  };

  const res = http.post(
    `${BASE_URL}/api/v1/pur/requisitions`,
    JSON.stringify(payload),
    { headers, tags: { operation: 'create requisition' } },
  );

  const success = check(res, {
    'create requisition status 201 or 200': (r) => r.status === 200 || r.status === 201,
  });
  apiSuccess.add(success ? 1 : 0);
  apiErrors.add(success ? 0 : 1);
  errorRate.add(!success);
  writeDuration.add(res.timings.duration);
}

function createInventoryTransaction(headers) {
  const payload = {
    companyId: COMPANY_ID,
    itemId: '00000000-0000-0000-0000-000000000001',
    warehouseId: '00000000-0000-0000-0000-000000000001',
    transactionType: 'Adjustment',
    quantity: Math.ceil(Math.random() * 5),
    reference: `Load Test TXN ${Date.now()}`,
  };

  const res = http.post(
    `${BASE_URL}/api/v1/inv/transactions`,
    JSON.stringify(payload),
    { headers, tags: { operation: 'create inventory transaction' } },
  );

  const success = check(res, {
    'create inventory txn not 500': (r) => r.status !== 500,
  });
  apiSuccess.add(success ? 1 : 0);
  apiErrors.add(success ? 0 : 1);
  errorRate.add(!success);
  writeDuration.add(res.timings.duration);
}

// ─── Teardown ────────────────────────────────────────────────────────────────
export function teardown(data) {
  console.log('─── Load Test Complete ───');
  console.log(`Total VUs: ${__VU}`);
  console.log(`Duration: ${__ENV.K6_DURATION || '45m'}`);
}
