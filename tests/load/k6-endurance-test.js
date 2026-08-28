/**
 * k6 Endurance (Soak) Test — 24-Hour Duration
 *
 * Runs 200 concurrent users for 24 hours to detect:
 * - Memory leaks
 * - Connection leaks
 * - Disk space issues
 * - Performance degradation over time
 *
 * Run:
 *   k6 run --vus 200 --duration 24h tests/load/k6-endurance-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6/metrics';
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const COMPANY_ID = __ENV.COMPANY_ID || '00000000-0000-0000-0000-000000000001';

const apiErrors = new Counter('api_errors');
const errorRate = new Rate('error_rate');
const readDuration = new Trend('read_duration');
const writeDuration = new Trend('write_duration');
const timeSinceStart = new Gauge('time_since_start_ms');

const startTime = Date.now();

export const options = {
  stages: [
    { duration: '10m', target: 200 },   // Ramp up
    { duration: '23h 40m', target: 200 }, // Sustain for 23h 40m
    { duration: '10m', target: 0 },       // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    error_rate: ['rate<0.001'],
  },
};

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/v1/platform/auth/login`,
    JSON.stringify({ username: 'admin', password: 'P@ssw0rd123!' }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status === 200) {
    const body = JSON.parse(loginRes.body);
    return { token: body.data?.token || body.token };
  }
  return { token: null };
}

export default function (data) {
  if (!data.token) return;

  timeSinceStart.add(Date.now() - startTime);

  const headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${data.token}`,
  };

  // Realistic workload: 80/20 read/write
  if (Math.random() < 0.8) {
    group('Read Workload', () => {
      const endpoints = [
        '/api/v1/platform/companies',
        '/api/v1/gl/accounts',
        '/api/v1/ap/vendors',
        '/api/v1/ar/customers',
        '/api/v1/inventory/items',
        '/api/v1/gl/journal-batches',
        '/api/v1/cash/bank-accounts',
        '/api/v1/platform/fiscal-periods',
        '/api/v1/payroll/employees',
        '/api/v1/platform/audit-logs',
      ];

      const path = endpoints[Math.floor(Math.random() * endpoints.length)];
      const res = http.get(`${BASE_URL}${path}`, { headers });

      check(res, {
        'status 200': (r) => r.status === 200,
      });

      apiErrors.add(res.status !== 200 ? 1 : 0);
      errorRate.add(res.status !== 200);
      readDuration.add(res.timings.duration);
    });
  } else {
    group('Write Workload', () => {
      const payload = {
        companyId: COMPANY_ID,
        description: `Endurance Test ${Date.now()}`,
        batchDate: new Date().toISOString(),
      };

      const res = http.post(
        `${BASE_URL}/api/v1/gl/journal-batches`,
        JSON.stringify(payload),
        { headers },
      );

      const success = check(res, {
        'not 500': (r) => r.status !== 500,
      });

      apiErrors.add(success ? 0 : 1);
      errorRate.add(!success);
      writeDuration.add(res.timings.duration);
    });
  }

  sleep(Math.random() * 2 + 1); // 1-3s think time (more realistic for endurance)
}

export function teardown(data) {
  const elapsed = (Date.now() - startTime) / 1000 / 60 / 60;
  console.log(`─── Endurance Test Complete: ${elapsed.toFixed(1)} hours ───`);
}
