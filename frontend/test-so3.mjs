import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: false });
const page = await browser.newPage();
page.on('console', msg => console.log('BROWSER', msg.text()));
page.on('pageerror', err => console.log('PAGEERROR', err.message));
await page.goto('http://localhost:3000/login', { waitUntil: 'domcontentloaded' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForURL('**/dashboard', { timeout: 10000 });
console.log('Logged in, URL:', page.url());
await page.goto('http://localhost:3000/om/sales-orders/new', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(3000);
console.log('On new SO, URL:', page.url());
let hasForm = await page.locator('form').count();
console.log('Has form:', hasForm);
// Fill customer
let customerInput = page.locator('input[placeholder="Search customer..."]');
await customerInput.click();
await customerInput.fill('C-');
await page.waitForTimeout(1000);
let opts = await page.locator('button:has-text("C-")').count();
console.log('Customer opts:', opts);
if (opts > 0) {
  await page.locator('button:has-text("C-")').first().click();
  await page.waitForTimeout(500);
  console.log('Selected customer');
}
let customerIdVal = await page.evaluate(() => {
  const el = document.querySelector('input[type="hidden"]');
  return el ? el.value : 'no hidden';
});
console.log('Hidden customerId:', customerIdVal);
// Check form values via react-hook-form
let formVal = await page.evaluate(() => {
  const form = document.querySelector('form');
  return form ? form.innerHTML.slice(0, 500) : 'no form';
});
console.log('Form snippet:', formVal.slice(0, 500));
await page.waitForTimeout(2000);
await browser.close();
