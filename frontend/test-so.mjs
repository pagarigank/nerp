import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on('console', msg => console.log('BROWSER', msg.type(), msg.text()));
page.on('pageerror', err => console.log('PAGEERROR', err.message));
page.on('request', r => { if (r.url().includes('/api/')) console.log('REQ', r.method(), r.url().slice(0,120)) });
page.on('response', r => { if (r.url().includes('/api/')) console.log('RES', r.status(), r.url().slice(0,120)) });

// Login
await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForTimeout(5000);
console.log('After login URL:', page.url());
console.log('Login check:', page.url().includes('dashboard') ? 'OK' : 'FAIL');

// Go to new SO
await page.goto('http://localhost:3000/om/sales-orders/new', { waitUntil: 'networkidle' });
await page.waitForTimeout(2000);
console.log('On new SO URL:', page.url());
let html = await page.content();
console.log('Has Order Number field:', html.includes('Order Number') ? 'YES' : 'NO');
console.log('Has Customer field:', html.includes('Customer') ? 'YES' : 'NO');

// Try to create without filling - should show validation
await page.click('button:has-text("Create Sales Order")');
await page.waitForTimeout(2000);
let afterClickHtml = await page.content();
console.log('After click without data - has error:', afterClickHtml.includes('required') || afterClickHtml.includes('Customer is required') ? 'YES' : 'NO');

// Fill customer
const customerInput = page.locator('input[placeholder="Search customer..."]');
await customerInput.click();
await customerInput.fill('Test');
await page.waitForTimeout(1000);
let dropdown = await page.locator('button:has-text("Test")').count();
console.log('Customer dropdown count:', dropdown);
if (dropdown > 0) {
  await page.locator('button:has-text("Test")').first().click();
  await page.waitForTimeout(1000);
  console.log('Selected customer, input value:', await customerInput.inputValue());
}

// Try to add a line - need to select warehouse, item etc.
let addLineBtn = await page.locator('button:has-text("Add Line")').count();
console.log('Add Line btn count:', addLineBtn);
if (addLineBtn > 0) {
  await page.click('button:has-text("Add Line")');
  await page.waitForTimeout(1000);
  console.log('Added line, lines count:', await page.locator('input[placeholder="Description"]').count());
}

// Try to create again
await page.click('button:has-text("Create Sales Order")');
await page.waitForTimeout(3000);
console.log('After second click URL:', page.url());
let finalHtml = await page.content();
console.log('Final has error:', finalHtml.includes('error') ? 'YES' : 'NO');
console.log('Final URL:', page.url());

await browser.close();
