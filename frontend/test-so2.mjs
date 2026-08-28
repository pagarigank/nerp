import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on('console', msg => console.log('BROWSER', msg.type(), msg.text()));
await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForTimeout(5000);
console.log('After login URL:', page.url());
let ls = await page.evaluate(() => localStorage.getItem('erp-auth-storage'));
console.log('LS after login:', ls ? ls.slice(0, 800) : 'null');
let auth = await page.evaluate(() => {
  try {
    const raw = localStorage.getItem('erp-auth-storage');
    const parsed = raw ? JSON.parse(raw) : null;
    const state = parsed?.state || parsed || {};
    return { isAuthenticated: state.isAuthenticated, hasToken: !!state.accessToken, tokenLen: state.accessToken ? state.accessToken.length : 0, user: state.user ? state.user.email : null };
  } catch(e) { return { error: e.message } }
});
console.log('Auth state:', JSON.stringify(auth));
await page.goto('http://localhost:3000/om/sales-orders/new', { waitUntil: 'networkidle' });
await page.waitForTimeout(3000);
console.log('On new SO URL:', page.url());
let html = await page.content();
console.log('Has Order Number:', html.includes('Order Number') ? 'YES' : 'NO');
console.log('Has Customer:', html.includes('Customer') ? 'YES' : 'NO');
console.log('HTML snippet:', html.slice(html.indexOf('Order Information')-200, html.indexOf('Order Information')+500).replace(/\n/g, ' ').slice(0,800));
await browser.close();
