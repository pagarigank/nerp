import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
let loginBody = null;
page.on('response', async r => {
  if (r.url().includes('/api/v1/auth/login')) {
    try { loginBody = await r.json(); console.log('LOGIN RES JSON:', JSON.stringify(loginBody).slice(0, 800)); } catch(e){ console.log('LOGIN RES text:', await r.text()) }
  }
});
page.on('console', msg => console.log('BROWSER', msg.type(), msg.text()));
page.on('pageerror', err => console.log('PAGEERROR', err.message));
await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForTimeout(5000);
console.log('URL:', page.url());
const ls = await page.evaluate(() => localStorage.getItem('erp-auth-storage'));
console.log('LS:', ls ? ls.slice(0, 1500) : 'null');
const errors = await page.evaluate(() => document.documentElement.innerHTML.slice(0, 3000));
console.log('HTML snippet:', errors.slice(0, 1500));
await browser.close();
