import { test, expect } from '@playwright/test';

test.describe('End-to-End Back-Office Admin & Audit Journey', () => {
  test('Complete Admin Flow: Login -> View Customer -> Freeze -> Verify Transfer Rejection -> Unfreeze -> Audit Log -> Run Reconciliation', async ({
    page,
    request,
  }) => {
    // 1. Admin Login
    await page.goto('/auth/login');
    await expect(page.locator('h1')).toContainText('Sign in to ApexCore');

    await page.fill('input[formControlName="email"]', 'admin@bank.local');
    await page.fill('input[formControlName="password"]', 'Admin@Pass1234!');
    await page.click('button[type="submit"]');

    // Wait for Admin Portal
    await page.waitForURL('**/admin/dashboard**', { timeout: 15000 });
    await expect(page.locator('h1')).toBeVisible();

    // 2. View Customer Management & Customer Detail
    await page.goto('/admin/customers');
    await expect(page.locator('h1')).toContainText('Customer Directory');

    // Table should list customers
    const customerRow = page.locator('table tbody tr').first();
    await customerRow.waitFor({ state: 'visible', timeout: 10000 });
    await expect(customerRow).toBeVisible();

    // Click on customer to view details
    await customerRow.locator('a, button').first().click();
    await expect(page.locator('text=Customer Profile')).toBeVisible({ timeout: 10000 });

    // 3. Freeze Account in Account Controls
    await page.goto('/admin/accounts');
    await expect(page.locator('h1')).toContainText('Account Controls');

    // Find first active customer account row
    const targetRow = page.locator('table tbody tr', { hasText: 'ACTIVE' }).first();
    await targetRow.waitFor({ state: 'visible', timeout: 10000 });

    const accountNumber = await targetRow.locator('td').first().innerText();
    const freezeBtn = targetRow.locator('button:has-text("Freeze Account")');
    await freezeBtn.click();

    // Fill freeze reason in modal
    const freezeModal = page.locator('app-modal');
    await freezeModal.waitFor({ state: 'visible' });
    await page.fill('input[placeholder*="Suspicious"], textarea', 'Suspected fraud / compliance review');
    await page.click('button:has-text("Confirm Immediate Freeze")');

    // Verify account status becomes FROZEN
    const frozenRow = page.locator('table tbody tr', { hasText: accountNumber.trim() });
    await expect(frozenRow.locator('text=FROZEN')).toBeVisible({ timeout: 10000 });

    // 4. Verify Transfer Rejection when Account is Frozen (via API check)
    // Obtain Afroza access token to verify debit is rejected
    const aliceLoginResp = await request.post('/api/v1/auth/login', {
      data: {
        email: 'afroza.begum@bank.local',
        password: 'Customer@Pass1234!',
      },
    });

    if (aliceLoginResp.ok()) {
      const aliceData = await aliceLoginResp.json();
      const accountsResp = await request.get('/api/v1/accounts', {
        headers: { Authorization: `Bearer ${aliceData.accessToken}` },
      });
      const aliceAccounts = await accountsResp.json();
      const frozenAcc = aliceAccounts.find((a: any) => a.status === 'FROZEN');

      if (frozenAcc) {
        // Attempt withdrawal/transfer from the frozen account -> must be rejected
        const transferAttempt = await request.post('/api/v1/withdrawals', {
          headers: {
            Authorization: `Bearer ${aliceData.accessToken}`,
            'Idempotency-Key': `e2e-frozen-check-${Date.now()}`,
          },
          data: {
            accountId: frozenAcc.id,
            amount: 100.0,
            currency: 'BDT',
            description: 'Attempt debit from frozen account',
          },
        });

        expect(transferAttempt.status()).toBeGreaterThanOrEqual(400);
        const errJson = await transferAttempt.json();
        expect(JSON.stringify(errJson)).toContain('ACCOUNT_FROZEN');
      }
    }

    // 5. Unfreeze Account
    await page.goto('/admin/accounts');
    const unfrozenRow = page.locator('table tbody tr', { hasText: accountNumber.trim() });
    const unfreezeBtn = unfrozenRow.locator('button:has-text("Unfreeze Account")');
    await unfreezeBtn.click();

    // Verify status returns to ACTIVE
    await expect(unfrozenRow.locator('text=ACTIVE')).toBeVisible({ timeout: 10000 });

    // 6. Audit Log Search
    await page.goto('/admin/audit-logs');
    await expect(page.locator('h1')).toContainText('Security & Regulatory Audit Trail');

    // Verify audit table contains logs
    await expect(page.locator('table tbody tr')).not.toHaveCount(0, { timeout: 10000 });

    // Filter by action
    await page.selectOption('select[formControlName="action"]', { label: 'ACCOUNT_FROZEN' });
    await page.click('button[type="submit"]');
    await expect(page.locator('table tbody')).toBeVisible();

    // 7. Run Reconciliation
    await page.goto('/admin/reconciliation');
    await expect(page.locator('h1')).toContainText('Ledger Reconciliation');

    // Verify trial balance card is visible
    await expect(page.locator('text=Trial Balance Verification')).toBeVisible({ timeout: 10000 });

    // Trigger reconciliation run
    const triggerBtn = page.locator('button:has-text("Trigger On-Demand Reconciliation")');
    await triggerBtn.click();

    // Verify success toast/message or updated run
    await expect(page.locator('text=Reconciliation Completed')).toBeVisible({ timeout: 20000 });
  });
});
