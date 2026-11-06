import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../../core/services/account.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { AccountType } from '../../../../core/models/account.models';

@Component({
  selector: 'app-open-account-wizard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="max-w-3xl mx-auto space-y-6">
      <div class="flex items-center gap-3">
        <a
          routerLink="/customer/accounts"
          class="p-2 rounded-xl text-slate-500 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        >
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Open a Bank Account</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Choose the right account product for your personal or commercial finances
          </p>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl space-y-8">
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-6">
          <!-- Step 1: Choose Account Type -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-3">
              1. Select Account Product
            </label>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <!-- Savings Card -->
              <div
                (click)="setAccountType('SAVINGS')"
                class="cursor-pointer p-5 rounded-2xl border-2 transition-all relative"
                [ngClass]="{
                  'border-sky-600 bg-sky-50/50 dark:bg-sky-950/40 shadow-md': selectedType() === 'SAVINGS',
                  'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700': selectedType() !== 'SAVINGS'
                }"
              >
                <div class="flex items-center justify-between mb-2">
                  <span class="text-sm font-bold text-slate-900 dark:text-white">Savings Account</span>
                  @if (selectedType() === 'SAVINGS') {
                    <span class="w-5 h-5 rounded-full bg-sky-600 text-white flex items-center justify-center text-xs">✓</span>
                  }
                </div>
                <p class="text-xs text-slate-500 dark:text-slate-400 mb-3">
                  Earn competitive monthly interest on your balances while retaining instant liquidity.
                </p>
                <ul class="text-[11px] text-slate-600 dark:text-slate-300 space-y-1">
                  <li>• Minimum Balance: <strong>৳ 500.00 BDT</strong></li>
                  <li>• Safe from negative balance</li>
                  <li>• Free intra-bank transfers</li>
                </ul>
              </div>

              <!-- Current Card -->
              <div
                (click)="setAccountType('CURRENT')"
                class="cursor-pointer p-5 rounded-2xl border-2 transition-all relative"
                [ngClass]="{
                  'border-sky-600 bg-sky-50/50 dark:bg-sky-950/40 shadow-md': selectedType() === 'CURRENT',
                  'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700': selectedType() !== 'CURRENT'
                }"
              >
                <div class="flex items-center justify-between mb-2">
                  <span class="text-sm font-bold text-slate-900 dark:text-white">Current Account</span>
                  @if (selectedType() === 'CURRENT') {
                    <span class="w-5 h-5 rounded-full bg-sky-600 text-white flex items-center justify-center text-xs">✓</span>
                  }
                </div>
                <p class="text-xs text-slate-500 dark:text-slate-400 mb-3">
                  Ideal for businesses and frequent daily transfers with high volume transactions.
                </p>
                <ul class="text-[11px] text-slate-600 dark:text-slate-300 space-y-1">
                  <li>• Minimum Balance: <strong>৳ 0.00 BDT</strong></li>
                  <li>• High daily transaction ceilings</li>
                  <li>• Unlimited debit/credit lines</li>
                </ul>
              </div>
            </div>
          </div>

          <!-- Step 2: Financial Configuration -->
          <div class="space-y-4 pt-4 border-t border-slate-100 dark:border-slate-800">
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              2. Initial Opening Deposit & Branch
            </label>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-600 dark:text-slate-300 mb-1">
                  Initial Deposit Amount (BDT)
                </label>
                <div class="relative">
                  <span class="absolute left-3 top-2.5 text-sm font-bold text-slate-400">৳</span>
                  <input
                    type="number"
                    step="0.01"
                    formControlName="initialDeposit"
                    placeholder="1000.00"
                    class="w-full pl-8 pr-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
                @if (selectedType() === 'SAVINGS') {
                  <span class="text-[11px] text-slate-400 mt-1 block">Must be at least ৳500 for Savings Account.</span>
                }
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-600 dark:text-slate-300 mb-1">
                  Operating Currency
                </label>
                <input
                  type="text"
                  value="BDT — Bangladeshi Taka"
                  disabled
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-100 dark:bg-slate-800/40 text-slate-500 dark:text-slate-400 text-sm font-medium"
                />
              </div>
            </div>
          </div>

          <button
            type="submit"
            [disabled]="form.invalid || loading()"
            class="w-full py-3 px-4 rounded-xl text-sm font-semibold text-white bg-sky-600 hover:bg-sky-500 active:bg-sky-700 shadow-lg shadow-sky-600/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2"
          >
            @if (loading()) {
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Generating Luhn Account Number & Posting Journal...</span>
            } @else {
              <span>Confirm & Open Account</span>
            }
          </button>
        </form>
      </div>
    </div>
  `,
})
export class OpenAccountWizardComponent {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  public selectedType = signal<AccountType>('SAVINGS');
  public loading = signal(false);

  public form = this.fb.group({
    accountType: ['SAVINGS', [Validators.required]],
    initialDeposit: ['1000.00', [Validators.required, Validators.min(0)]],
    currency: ['BDT', [Validators.required]],
  });

  public setAccountType(type: AccountType): void {
    this.selectedType.set(type);
    this.form.patchValue({ accountType: type });
    if (type === 'SAVINGS') {
      this.form.get('initialDeposit')?.setValidators([Validators.required, Validators.min(500)]);
    } else {
      this.form.get('initialDeposit')?.setValidators([Validators.required, Validators.min(0)]);
    }
    this.form.get('initialDeposit')?.updateValueAndValidity();
  }

  public onSubmit(): void {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    const val = this.form.value;

    this.accountService
      .openAccount({
        accountType: val.accountType as AccountType,
        initialDeposit: val.initialDeposit!,
        currency: 'BDT',
      })
      .subscribe({
        next: (account) => {
          this.loading.set(false);
          this.notification.success(
            'Account Opened',
            `Your new ${account.accountType} account (${account.accountNumber}) has been provisioned successfully.`
          );
          this.router.navigate(['/customer/accounts']);
        },
        error: () => this.loading.set(false),
      });
  }
}
