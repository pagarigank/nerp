import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on('console', msg => console.log('BROWSER', msg.text()));
page.on('request', r => { if (r.url().includes('/api/')) console.log('REQ', r.method(), r.url()) });
page.on('response', r => { if (r.url().includes('/api/')) console.log('RES', r.status(), r.url()) });
await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForTimeout(4000);
console.log('URL after login:', page.url());
const ls = await page.evaluate(() => localStorage.getItem('erp-auth-storage'));
console.log('LS exists:', !!ls);
if (ls) {
  const parsed = JSON.parse(ls);
  const state = parsed.state || parsed;
  console.log('isAuthenticated:', state.isAuthenticated, 'hasToken:', !!state.accessToken);
}
const content = await page.content();
console.log('On dashboard:', content.includes('Dashboard') || content.includes('US Operations'));
await page.screenshot({ path: 'D:/nerp/login-test.png' });
console.log('Screenshot saved');
await browser.close();
