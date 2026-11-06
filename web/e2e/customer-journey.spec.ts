import { test, expect } from '@playwright/test';

test.describe('End-to-End Customer Banking Journey', () => {
  const timestamp = Date.now();
  const testCustomer = {
    fullName: `Tareq Chowdhury ${timestamp}`,
    email: `tareq_${timestamp}@apexbank.local`,
    password: 'Customer@Pass1234!',
    phone: '+8801712345678',
    dateOfBirth: '1992-08-15',
    address: 'House 42, Road 11, Banani, Dhaka-1213',
  };

  test('Complete Customer Flow: Register -> Verify -> Login -> Open Account -> Deposit -> Transfer -> History -> Export CSV', async ({
    page,
  }) => {
    // 1. Registration
    await page.goto('/auth/register');
    await expect(page.locator('h1')).toContainText('Open Customer Account');

    await page.fill('input[formControlName="fullName"]', testCustomer.fullName);
    await page.fill('input[formControlName="email"]', testCustomer.email);
    await page.fill('input[formControlName="phone"]', testCustomer.phone);
    await page.fill('input[formControlName="dateOfBirth"]', testCustomer.dateOfBirth);
    await page.fill('input[formControlName="password"]', testCustomer.password);
    await page.fill('textarea[formControlName="address"]', testCustomer.address);

    await page.click('button[type="submit"]');

    // 2. Verification
    await page.waitForURL('**/auth/verify-email**', { timeout: 15000 });
    await expect(page.locator('h1')).toContainText('Verify Your Email');

    // Fill verification token
    await page.fill('input[formControlName="token"]', 'DEV-VERIFY-123456');
    await page.click('button[type="submit"]');

    // 3. Login
    await page.waitForURL('**/auth/login**', { timeout: 15000 });
    await expect(page.locator('h1')).toContainText('Sign in to ApexCore');

    await page.fill('input[formControlName="email"]', testCustomer.email);
    await page.fill('input[formControlName="password"]', testCustomer.password);
    await page.click('button[type="submit"]');

    // 4. Customer Dashboard & Open Account
    await page.waitForURL('**/customer/dashboard**', { timeout: 15000 });
    await expect(page.locator('h1')).toBeVisible();

    // Navigate to Open Account wizard
    await page.goto('/customer/accounts/open');
    await expect(page.locator('h1')).toContainText('Open a Bank Account');

    // Select Savings product
    await page.click('div:has-text("Savings Account")');
    await page.fill('input[formControlName="overdraftLimit"]', '0');
    await page.fill('input[formControlName="minBalance"]', '500');
    await page.click('button[type="submit"]');

    // Wait for account creation and redirect
    await page.waitForURL('**/customer/accounts**', { timeout: 15000 });
    await expect(page.locator('text=SAVINGS')).toBeVisible({ timeout: 10000 });

    // 5. Deposit Money
    await page.goto('/customer/payments/deposit');
    await expect(page.locator('h1')).toContainText('Cash Deposit');

    // Select account from dropdown
    const depositSelect = page.locator('select[formControlName="accountId"]');
    await depositSelect.waitFor({ state: 'visible' });
    const options = await depositSelect.locator('option:not([disabled])').all();
    expect(options.length).toBeGreaterThan(0);
    const targetAccountId = await options[0].getAttribute('value');
    await depositSelect.selectOption(targetAccountId!);

    await page.fill('input[formControlName="amount"]', '5000');
    await page.fill('input[formControlName="description"]', 'E2E Initial cash deposit');
    await page.click('button[type="submit"]');

    // Verify Deposit Confirmation Modal and Confirm
    const modalConfirmBtn = page.locator('button:has-text("Confirm & Post Deposit")');
    if (await modalConfirmBtn.isVisible({ timeout: 3000 })) {
      await modalConfirmBtn.click();
    }
    await expect(page.locator('text=Deposit Successfully Posted')).toBeVisible({ timeout: 15000 });

    // 6. Transfer Money (Intra-Bank Transfer to Alice demo account)
    await page.goto('/customer/payments/transfer');
    await expect(page.locator('h1')).toContainText('Intra-Bank Transfer');

    const sourceSelect = page.locator('select[formControlName="sourceAccountId"]');
    await sourceSelect.waitFor({ state: 'visible' });
    await sourceSelect.selectOption(targetAccountId!);

    // Beneficiary account number: using demo account 1001000000018
    await page.fill('input[formControlName="destinationAccountNumber"]', '1001000000018');
    await page.fill('input[formControlName="amount"]', '750');
    await page.fill('input[formControlName="description"]', 'E2E Transfer test');

    // Click Review Transfer
    await page.click('button:has-text("Review & Confirm Transfer")');

    // Confirm in Modal
    const confirmTransferBtn = page.locator('button:has-text("Authorize & Post")');
    await confirmTransferBtn.waitFor({ state: 'visible', timeout: 5000 });
    await confirmTransferBtn.click();

    // Verify transfer receipt
    await expect(page.locator('text=Transfer Executed Successfully')).toBeVisible({ timeout: 15000 });

    // 7. Transaction History & CSV Export
    await page.goto('/customer/transactions');
    await expect(page.locator('h1')).toContainText('Transaction History');

    // Filter by our account
    const historyAccSelect = page.locator('select[formControlName="accountId"]');
    await historyAccSelect.waitFor({ state: 'visible' });
    await historyAccSelect.selectOption(targetAccountId!);

    // Verify ledger entries are displayed
    await expect(page.locator('table tbody tr')).not.toHaveCount(0, { timeout: 10000 });

    // 8. Export CSV Statement
    const downloadPromise = page.waitForEvent('download', { timeout: 10000 });
    await page.click('button:has-text("Download Statement (CSV)")');
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toContain('.csv');
  });
});
