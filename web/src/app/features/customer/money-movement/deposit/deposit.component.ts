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
import { ModalComponent } from '../../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-deposit',
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
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Cash Deposit</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Deposit cash directly into your savings or current account via teller/counter simulation
          </p>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl space-y-6">
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-5">
          <!-- Target Account -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              Select Destination Account
            </label>
            <select
              formControlName="accountId"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            >
              <option value="" disabled>Choose account to deposit funds</option>
              @for (acc of accounts(); track acc.id) {
                <option [value]="acc.id" [disabled]="acc.status !== 'ACTIVE'">
                  {{ acc.accountType }} - {{ acc.accountNumber }} (Available: {{ acc.availableBalance | money }})
                  @if (acc.status !== 'ACTIVE') { [{{ acc.status }}] }
                </option>
              }
            </select>
          </div>

          <!-- Deposit Amount -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              Deposit Amount (BDT)
            </label>
            <div class="relative">
              <span class="absolute left-3.5 top-2.5 text-base font-bold text-slate-400">৳</span>
              <input
                type="number"
                step="0.01"
                formControlName="amount"
                placeholder="5000.00"
                class="w-full pl-9 pr-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white font-mono text-base font-semibold focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>

            <!-- Quick Amounts -->
            <div class="flex items-center gap-2 mt-2">
              @for (q of [1000, 5000, 10000, 25000]; track q) {
                <button
                  type="button"
                  (click)="setQuickAmount(q)"
                  class="px-2.5 py-1 text-xs font-medium rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 transition-colors"
                >
                  +৳{{ q }}
                </button>
              }
            </div>
          </div>

          <!-- Notes -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              Counter Reference / Deposit Memo
            </label>
            <input
              type="text"
              formControlName="notes"
              placeholder="e.g., Monthly salary cash deposit"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            />
          </div>

          <!-- Idempotency Notice -->
          <div class="p-3 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs text-slate-500">
            <span class="flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
              Double-click protection active (UUIDv4 idempotency key attached)
            </span>
            <span class="font-mono text-[10px] text-slate-400 truncate max-w-[150px]">
              {{ currentIdempotencyKey() }}
            </span>
          </div>

          <button
            type="submit"
            [disabled]="form.invalid || loading()"
            class="w-full py-3 px-4 rounded-xl text-sm font-semibold text-white bg-emerald-600 hover:bg-emerald-500 active:bg-emerald-700 shadow-lg shadow-emerald-600/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2"
          >
            @if (loading()) {
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Executing Double-Entry Cash Journal...</span>
            } @else {
              <span>Confirm & Post Deposit</span>
            }
          </button>
        </form>
      </div>

      <!-- Deposit Success Receipt Modal -->
      <app-modal
        [isOpen]="showReceipt()"
        title="Deposit Receipt"
        [showFooter]="true"
        (close)="dismissReceipt()"
      >
        @if (receipt(); as r) {
          <div class="space-y-4 text-xs text-slate-700 dark:text-slate-300">
            <div class="p-4 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-center">
              <div class="w-10 h-10 rounded-full bg-emerald-100 dark:bg-emerald-900/60 text-emerald-600 mx-auto flex items-center justify-center mb-2">
                <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <div class="text-xs uppercase font-bold tracking-wider text-emerald-800 dark:text-emerald-300">
                Cash Deposit Posted
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
                <span class="text-slate-400">New Available Balance:</span>
                <span class="font-bold text-emerald-600">{{ r.balanceAfter | money }}</span>
              </div>
            </div>

            <p class="text-[11px] text-slate-400 text-center">
              Compensating journal entry registered under Cash Pool account.
            </p>
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
export class DepositComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private transactionService = inject(TransactionService);
  private idempotencyService = inject(IdempotencyService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  public accounts = signal<AccountDto[]>([]);
  public loading = signal(false);
  public currentIdempotencyKey = signal('');
  public receipt = signal<TransactionReceiptDto | null>(null);
  public showReceipt = signal(false);

  private readonly ACTION_TAG = 'cash_deposit_form';

  public form = this.fb.group({
    accountId: ['', [Validators.required]],
    amount: ['', [Validators.required, Validators.min(10)]],
    notes: ['Cash counter deposit', [Validators.required]],
  });

  ngOnInit(): void {
    this.currentIdempotencyKey.set(this.idempotencyService.getOrCreateKey(this.ACTION_TAG));

    this.accountService.getAccounts().subscribe({
      next: (data) => {
        this.accounts.set(data || []);
        const paramAcc = this.route.snapshot.queryParams['account'];
        if (paramAcc && data.some((a) => a.id === paramAcc)) {
          this.form.patchValue({ accountId: paramAcc });
        } else if (data.length > 0) {
          this.form.patchValue({ accountId: data[0].id });
        }
      },
    });
  }

  public setQuickAmount(amt: number): void {
    this.form.patchValue({ amount: amt.toString() });
  }

  public onSubmit(): void {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    const val = this.form.value;

    this.transactionService
      .deposit({
        accountId: val.accountId!,
        amount: val.amount!,
        currency: 'BDT',
        notes: val.notes || 'Counter cash deposit',
        channel: 'BRANCH_COUNTER',
      })
      .subscribe({
        next: (receiptRes) => {
          this.loading.set(false);
          this.receipt.set(receiptRes);
          this.showReceipt.set(true);
          this.idempotencyService.refreshKey(this.ACTION_TAG);
          this.currentIdempotencyKey.set(this.idempotencyService.getOrCreateKey(this.ACTION_TAG));
          this.notification.success('Deposit Successful', `Ref: ${receiptRes.reference}`);
        },
        error: () => this.loading.set(false),
      });
  }

  public dismissReceipt(): void {
    this.showReceipt.set(false);
    this.router.navigate(['/customer/dashboard']);
  }
}
