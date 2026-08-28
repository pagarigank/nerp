/**
 * k6 Stress Test — Find Breaking Point
 *
 * Ramps from 0 to 1000 concurrent users to find the system's breaking point.
 * Measures degradation curve under extreme load.
 *
 * Run:
 *   k6 run tests/load/k6-stress-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6/metrics';
import { Counter, Rate, Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const COMPANY_ID = __ENV.COMPANY_ID || '00000000-0000-0000-0000-000000000001';

const apiErrors = new Counter('api_errors');
const errorRate = new Rate('error_rate');
const readDuration = new Trend('read_duration');

export const options = {
  stages: [
    { duration: '1m', target: 100 },    // Phase 1: 0 → 100
    { duration: '3m', target: 100 },    // Phase 2: sustain 100
    { duration: '1m', target: 300 },    // Phase 3: 100 → 300
    { duration: '3m', target: 300 },    // Phase 4: sustain 300
    { duration: '1m', target: 500 },    // Phase 5: 300 → 500
    { duration: '5m', target: 500 },    // Phase 6: sustain 500
    { duration: '1m', target: 750 },    // Phase 7: 500 → 750
    { duration: '3m', target: 750 },    // Phase 8: sustain 750
    { duration: '1m', target: 1000 },   // Phase 9: 750 → 1000
    { duration: '5m', target: 1000 },   // Phase 10: sustain 1000
    { duration: '2m', target: 0 },      // Phase 11: ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],   // Relaxed: < 3s under stress
    error_rate: ['rate<0.05'],            // Relaxed: < 5% under stress
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

  const headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${data.token}`,
  };

  // Mixed read/write: 70/30 (more writes under stress)
  if (Math.random() < 0.7) {
    group('Read Under Stress', () => {
      const endpoints = [
        '/api/v1/platform/companies',
        '/api/v1/gl/accounts',
        '/api/v1/ap/vendors',
        '/api/v1/ar/customers',
        '/api/v1/inventory/items',
      ];
      const path = endpoints[Math.floor(Math.random() * endpoints.length)];

      const res = http.get(`${BASE_URL}${path}`, { headers });
      const success = check(res, {
        'status 200': (r) => r.status === 200,
        'response < 5s': (r) => r.timings.duration < 5000,
      });

      apiErrors.add(success ? 0 : 1);
      errorRate.add(!success);
      readDuration.add(res.timings.duration);
    });
  } else {
    group('Write Under Stress', () => {
      const payload = {
        companyId: COMPANY_ID,
        description: `Stress Test ${Date.now()}`,
        batchDate: new Date().toISOString(),
      };

      const res = http.post(
        `${BASE_URL}/api/v1/gl/journal-batches`,
        JSON.stringify(payload),
        { headers },
      );

      const success = check(res, {
        'not 500': (r) => r.status !== 500,
        'response < 5s': (r) => r.timings.duration < 5000,
      });

      apiErrors.add(success ? 0 : 1);
      errorRate.add(!success);
    });
  }

  sleep(Math.random() * 1 + 0.5);
}

export function teardown(data) {
  console.log('─── Stress Test Complete ───');
}
