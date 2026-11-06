import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AccountService } from '../../../../core/services/account.service';
import { TransactionService } from '../../../../core/services/transaction.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { IdempotencyService } from '../../../../core/services/idempotency.service';
import { AccountDto } from '../../../../core/models/account.models';
import { TransactionReceiptDto } from '../../../../core/models/transaction.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { MoneyUtil } from '../../../../shared/utils/money.util';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-withdrawal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MoneyPipe, ModalComponent],
  template: `
    <div class="max-w-2xl mx-auto space-y-6">
      <div class="flex items-center gap-3">
        <a
          routerLink="/customer/dashboard"
          class="p-2 rounded-xl text-slate-500 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        >
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Cash Withdrawal</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Withdraw funds from your account with real-time balance and limit checks
          </p>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl space-y-6">
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-5">
          <!-- Source Account -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              Select Source Account
            </label>
            <select
              formControlName="accountId"
              (change)="onAccountChange()"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            >
              <option value="" disabled>Choose account to debit</option>
              @for (acc of accounts(); track acc.id) {
                <option [value]="acc.id" [disabled]="acc.status !== 'ACTIVE'">
                  {{ acc.accountType }} - {{ acc.accountNumber }} (Available: {{ acc.availableBalance | money }})
                  @if (acc.status !== 'ACTIVE') { [{{ acc.status }}] }
                </option>
              }
            </select>
          </div>

          <!-- Live Balance Card -->
          @if (selectedAccount(); as acc) {
            <div class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200/70 dark:border-slate-800 flex items-center justify-between text-xs">
              <div>
                <span class="text-slate-400 block">Available Liquidity:</span>
                <span class="text-lg font-bold text-slate-900 dark:text-white">{{ acc.availableBalance | money }}</span>
              </div>
              <div class="text-right">
                <span class="text-slate-400 block">Daily Limit / Min Bal:</span>
                <span class="font-medium text-slate-700 dark:text-slate-300">Min {{ acc.minimumBalance | money }}</span>
              </div>
            </div>
          }

          <!-- Withdrawal Amount -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              Withdrawal Amount (BDT)
            </label>
            <div class="relative">
              <span class="absolute left-3.5 top-2.5 text-base font-bold text-slate-400">৳</span>
              <input
                type="number"
                step="0.01"
                formControlName="amount"
                (input)="checkAmountLimit()"
                placeholder="2000.00"
                class="w-full pl-9 pr-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white font-mono text-base font-semibold focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>

            @if (insufficientFundsWarning()) {
              <div class="mt-2 p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900/60 text-xs text-rose-800 dark:text-rose-300 flex items-center gap-2">
                <svg class="w-4 h-4 shrink-0 text-rose-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
                <span>Warning: Requested amount exceeds available liquidity + minimum balance requirement.</span>
              </div>
            }
          </div>

          <!-- Notes -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              Withdrawal Memo / Purpose
            </label>
            <input
              type="text"
              formControlName="notes"
              placeholder="e.g., Personal cash withdrawal"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            />
          </div>

          <button
            type="submit"
            [disabled]="form.invalid || loading() || insufficientFundsWarning()"
            class="w-full py-3 px-4 rounded-xl text-sm font-semibold text-white bg-amber-600 hover:bg-amber-500 active:bg-amber-700 shadow-lg shadow-amber-600/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2"
          >
            @if (loading()) {
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Verifying Ledger Balance & Debiting...</span>
            } @else {
              <span>Confirm & Process Withdrawal</span>
            }
          </button>
        </form>
      </div>

      <!-- Receipt Modal -->
      <app-modal
        [isOpen]="showReceipt()"
        title="Withdrawal Receipt"
        [showFooter]="true"
        (close)="dismissReceipt()"
      >
        @if (receipt(); as r) {
          <div class="space-y-4 text-xs text-slate-700 dark:text-slate-300">
            <div class="p-4 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 text-center">
              <div class="w-10 h-10 rounded-full bg-amber-100 dark:bg-amber-900/60 text-amber-600 mx-auto flex items-center justify-center mb-2">
                <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <div class="text-xs uppercase font-bold tracking-wider text-amber-800 dark:text-amber-300">
                Cash Withdrawal Dispatched
              </div>
              <div class="text-2xl font-black text-slate-900 dark:text-white mt-1">
                {{ r.amount | money }}
              </div>
            </div>

            <div class="space-y-2 border-t border-b border-slate-100 dark:border-slate-800 py-3">
              <div class="flex justify-between">
                <span class="text-slate-400">Reference:</span>
                <span class="font-mono font-bold text-slate-900 dark:text-white">{{ r.reference }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-400">Timestamp:</span>
                <span>{{ r.timestamp | date: 'medium' }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-400">Remaining Balance:</span>
                <span class="font-bold text-amber-600">{{ r.balanceAfter | money }}</span>
              </div>
            </div>
          </div>
        }

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="dismissReceipt()"
            class="w-full py-2 px-4 rounded-xl text-xs font-semibold text-white bg-slate-900 dark:bg-slate-700 hover:bg-slate-800 transition-colors"
          >
            Done
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class WithdrawalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private transactionService = inject(TransactionService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  public accounts = signal<AccountDto[]>([]);
  public selectedAccount = signal<AccountDto | null>(null);
  public loading = signal(false);
  public insufficientFundsWarning = signal(false);
  public receipt = signal<TransactionReceiptDto | null>(null);
  public showReceipt = signal(false);

  public form = this.fb.group({
    accountId: ['', [Validators.required]],
    amount: ['', [Validators.required, Validators.min(10)]],
    notes: ['Cash withdrawal at branch counter', [Validators.required]],
  });

  ngOnInit(): void {
    this.accountService.getAccounts().subscribe({
      next: (data) => {
        this.accounts.set(data || []);
        const paramAcc = this.route.snapshot.queryParams['account'];
        if (paramAcc && data.some((a) => a.id === paramAcc)) {
          this.form.patchValue({ accountId: paramAcc });
        } else if (data.length > 0) {
          this.form.patchValue({ accountId: data[0].id });
        }
        this.onAccountChange();
      },
    });
  }

  public onAccountChange(): void {
    const accId = this.form.get('accountId')?.value;
    const found = this.accounts().find((a) => a.id === accId) || null;
    this.selectedAccount.set(found);
    this.checkAmountLimit();
  }

  public checkAmountLimit(): void {
    const acc = this.selectedAccount();
    const amtStr = this.form.get('amount')?.value;
    if (!acc || !amtStr) {
      this.insufficientFundsWarning.set(false);
      return;
    }

    const available = acc.availableBalance || '0';
    const overdraft = acc.overdraftLimit || '0';
    const totalPossible = MoneyUtil.add(available, overdraft);

    if (MoneyUtil.isGreaterThan(amtStr, totalPossible)) {
      this.insufficientFundsWarning.set(true);
    } else {
      this.insufficientFundsWarning.set(false);
    }
  }

  public onSubmit(): void {
    if (this.form.invalid || this.loading() || this.insufficientFundsWarning()) return;

    this.loading.set(true);
    const val = this.form.value;

    this.transactionService
      .withdraw({
        accountId: val.accountId!,
        amount: val.amount!,
        currency: 'BDT',
        notes: val.notes || 'Cash withdrawal',
      })
      .subscribe({
        next: (receiptRes) => {
          this.loading.set(false);
          this.receipt.set(receiptRes);
          this.showReceipt.set(true);
          this.notification.success('Withdrawal Dispatched', `Ref: ${receiptRes.reference}`);
        },
        error: () => this.loading.set(false),
      });
  }

  public dismissReceipt(): void {
    this.showReceipt.set(false);
    this.router.navigate(['/customer/dashboard']);
  }
}
