import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: false });
const page = await browser.newPage();
page.on('console', msg => { if (msg.text().includes('SUBMIT') || msg.text().includes('LOGIN') || msg.text().includes('error')) console.log('BROWSER', msg.text()) });
page.on('pageerror', err => console.log('PAGEERROR', err.message));
page.on('request', r => { if (r.url().includes('/api/v1/om/sales-orders') && r.method() === 'POST') console.log('REQ POST SO', r.url()) });
page.on('response', r => { if (r.url().includes('/api/v1/om/sales-orders') && r.method() === 'POST') console.log('RES POST SO', r.status(), r.url()) });
await page.goto('http://localhost:3000/login', { waitUntil: 'domcontentloaded' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForURL('**/dashboard', { timeout: 10000 });
await page.goto('http://localhost:3000/om/sales-orders/new', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(2000);
// Select customer
let customerInput = page.locator('input[placeholder="Search customer..."]');
await customerInput.click();
await customerInput.fill('C-');
await page.waitForTimeout(1000);
await page.locator('button:has-text("C-")').first().click();
await page.waitForTimeout(500);
console.log('Customer selected');
// Add line
await page.click('button:has-text("Add Line")');
await page.waitForTimeout(500);
console.log('Add Line clicked');
// Select item - need to find the item dropdown
let itemSearch = page.locator('input[placeholder="Search item..."]');
let count = await itemSearch.count();
console.log('Item search inputs:', count);
if (count > 0) {
  await itemSearch.first().click();
  await itemSearch.first().fill('ITEM');
  await page.waitForTimeout(1000);
  let itemOpts = await page.locator('button:has-text("ITEM")').count();
  console.log('Item opts:', itemOpts);
  if (itemOpts > 0) {
    await page.locator('button:has-text("ITEM")').first().click();
    await page.waitForTimeout(500);
    console.log('Item selected');
  }
}
// Set quantity
let qtyInputs = await page.locator('input[type="number"]').count();
console.log('Number inputs:', qtyInputs);
// Click Create
await page.click('button:has-text("Create Sales Order")');
await page.waitForTimeout(3000);
console.log('After Create click URL:', page.url());
let html = await page.content();
console.log('Has error:', html.includes('error') || html.includes('Error') ? 'YES' : 'NO');
console.log('Has success:', html.includes('success') || html.includes('created') ? 'YES' : 'NO');
await page.waitForTimeout(2000);
await browser.close();
