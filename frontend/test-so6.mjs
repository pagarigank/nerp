import { chromium } from 'playwright';
const browser = await chromium.launch({ headless: false });
const page = await browser.newPage();
await page.goto('http://localhost:3000/login', { waitUntil: 'domcontentloaded' });
await page.fill('#email', 'demo@erp.com');
await page.fill('#password', 'password123');
await page.click('button[type="submit"]');
await page.waitForURL('**/dashboard', { timeout: 10000 });
await page.goto('http://localhost:3000/om/sales-orders/new', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(2000);
let customerInput = page.locator('input[placeholder="Search customer..."]');
await customerInput.click();
await customerInput.fill('C-');
await page.waitForTimeout(1000);
await page.locator('button:has-text("C-")').first().click();
await page.waitForTimeout(500);
await page.click('button:has-text("Add Line")');
await page.waitForTimeout(500);
let html = await page.content();
let orderLinesIdx = html.indexOf('Order Lines');
console.log('Order Lines idx:', orderLinesIdx);
if (orderLinesIdx !== -1) {
  console.log(html.slice(orderLinesIdx-300, orderLinesIdx+2000).replace(/\n/g, ' ').slice(0, 2000));
} else {
  console.log('No Order Lines found, full HTML length:', html.length);
  console.log(html.slice(0, 2000).replace(/\n/g, ' '));
}
await page.click('button:has-text("Create Sales Order")');
await page.waitForTimeout(2000);
let afterHtml = await page.content();
let errorIdx = afterHtml.toLowerCase().indexOf('error');
console.log('Error idx:', errorIdx);
if (errorIdx !== -1) console.log(afterHtml.slice(errorIdx-500, errorIdx+1000).replace(/\n/g, ' ').slice(0, 1500));
else console.log('No error found, has formError?', afterHtml.includes('formError') ? 'YES' : 'NO');
await browser.close();
