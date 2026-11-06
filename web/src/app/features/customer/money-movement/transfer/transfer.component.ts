import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AccountService } from '../../../../core/services/account.service';
import { TransactionService } from '../../../../core/services/transaction.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { IdempotencyService } from '../../../../core/services/idempotency.service';
import { AccountDto, BeneficiaryLookupDto } from '../../../../core/models/account.models';
import { TransactionReceiptDto } from '../../../../core/models/transaction.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { MoneyUtil } from '../../../../shared/utils/money.util';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-transfer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MoneyPipe, ModalComponent, BadgeComponent],
  template: `
    <div class="max-w-3xl mx-auto space-y-6">
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
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Intra-Bank Transfer</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Real-time, double-entry transfer between ApexCore bank accounts with Luhn check digit validation
          </p>
        </div>
      </div>

      <!-- Main Form View (shown when no receipt) -->
      @if (!receipt()) {
        <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl space-y-6">
          <form [formGroup]="form" class="space-y-6">
            <!-- Source Account -->
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                From Account
              </label>
              <select
                formControlName="sourceAccountId"
                (change)="onSourceAccountChange()"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              >
                <option value="" disabled>Select debit account</option>
                @for (acc of accounts(); track acc.id) {
                  <option [value]="acc.id" [disabled]="acc.status !== 'ACTIVE'">
                    {{ acc.accountType }} — {{ acc.accountNumber }} (Available: {{ acc.availableBalance | money }})
                    @if (acc.status !== 'ACTIVE') { [{{ acc.status }}] }
                  </option>
                }
              </select>

              @if (selectedSource(); as src) {
                <div class="mt-2 p-3 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-800 flex items-center justify-between text-xs">
                  <span class="text-slate-500 dark:text-slate-400">Available to transfer:</span>
                  <span class="font-bold text-slate-900 dark:text-white">{{ src.availableBalance | money }}</span>
                </div>
              }
            </div>

            <!-- Destination Beneficiary Account -->
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300">
                  Beneficiary Account Number
                </label>
                @if (lookingUpBeneficiary()) {
                  <span class="text-xs text-sky-600 flex items-center gap-1">
                    <svg class="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <span>Verifying beneficiary...</span>
                  </span>
                }
              </div>

              <div class="relative">
                <input
                  type="text"
                  formControlName="destinationAccountNumber"
                  (input)="onBeneficiaryInput()"
                  placeholder="e.g. 1001234567890"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white font-mono text-sm tracking-wider focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
                <button
                  type="button"
                  (click)="lookupBeneficiary()"
                  class="absolute right-2 top-2 px-3 py-1 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 rounded-lg text-xs font-semibold text-slate-700 dark:text-slate-200 transition-colors"
                >
                  Verify
                </button>
              </div>

              <!-- Beneficiary Preview Card -->
              @if (beneficiary(); as ben) {
                <div class="mt-3 p-4 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800/80 flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-9 h-9 rounded-full bg-emerald-100 dark:bg-emerald-900/60 text-emerald-700 dark:text-emerald-300 font-bold text-sm flex items-center justify-center">
                      {{ ben.accountHolderName.charAt(0) | uppercase }}
                    </div>
                    <div>
                      <div class="text-xs uppercase font-bold text-emerald-800 dark:text-emerald-300">
                        Beneficiary Confirmed
                      </div>
                      <div class="text-sm font-semibold text-slate-900 dark:text-white">
                        {{ ben.accountHolderName }}
                      </div>
                      <div class="text-[11px] font-mono text-slate-500">
                        {{ ben.accountType }} · {{ ben.accountNumber }}
                      </div>
                    </div>
                  </div>
                  <app-badge variant="success">Active Beneficiary</app-badge>
                </div>
              } @else if (beneficiaryError()) {
                <div class="mt-2 text-xs text-rose-500 font-medium flex items-center gap-1.5">
                  <svg class="w-4 h-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span>{{ beneficiaryError() }}</span>
                </div>
              }
            </div>

            <!-- Amount -->
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Transfer Amount (BDT)
              </label>
              <div class="relative">
                <span class="absolute left-3.5 top-2.5 text-base font-bold text-slate-400">৳</span>
                <input
                  type="number"
                  step="0.01"
                  formControlName="amount"
                  (input)="checkAmountLimit()"
                  placeholder="2500.00"
                  class="w-full pl-9 pr-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white font-mono text-base font-semibold focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>

              <!-- Quick Amounts -->
              <div class="flex items-center gap-2 mt-2">
                @for (q of [500, 1000, 5000, 10000]; track q) {
                  <button
                    type="button"
                    (click)="setQuickAmount(q)"
                    class="px-2.5 py-1 text-xs font-medium rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 transition-colors"
                  >
                    ৳{{ q }}
                  </button>
                }
              </div>

              @if (insufficientBalanceWarning()) {
                <div class="mt-2 p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900/60 text-xs text-rose-800 dark:text-rose-300">
                  ⚠️ Transfer amount exceeds available balance for the selected source account.
                </div>
              }
            </div>

            <!-- Reference / Memo -->
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Payment Reference Note
              </label>
              <input
                type="text"
                formControlName="referenceNote"
                placeholder="e.g. Office rent share / freelance payment"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>

            <!-- Idempotency Key Badge -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200/80 dark:border-slate-800 text-xs space-y-1">
              <div class="flex items-center justify-between text-slate-600 dark:text-slate-400">
                <span class="font-semibold flex items-center gap-1.5">
                  <span class="w-2 h-2 rounded-full bg-sky-500"></span>
                  Idempotency-Key Header
                </span>
                <button
                  type="button"
                  (click)="refreshIdempotencyKey()"
                  class="text-[11px] text-sky-600 dark:text-sky-400 hover:underline"
                >
                  Regenerate
                </button>
              </div>
              <div class="font-mono text-[11px] text-slate-500 dark:text-slate-400 break-all select-all">
                {{ idempotencyKey() }}
              </div>
            </div>

            <button
              type="button"
              (click)="openConfirmationModal()"
              [disabled]="form.invalid || insufficientBalanceWarning() || !beneficiary()"
              class="w-full py-3 px-4 rounded-xl text-sm font-semibold text-white bg-sky-600 hover:bg-sky-500 active:bg-sky-700 shadow-lg shadow-sky-600/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2"
            >
              <span>Review & Authorize Transfer</span>
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
              </svg>
            </button>
          </form>
        </div>
      } @else {
        <!-- Transfer Receipt View -->
        <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl space-y-6 animate-in zoom-in-95">
          <div class="text-center">
            <div class="w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-950/80 text-emerald-600 mx-auto flex items-center justify-center mb-4 ring-8 ring-emerald-50 dark:ring-emerald-900/20">
              <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <span class="text-xs uppercase font-bold tracking-wider text-emerald-600 dark:text-emerald-400">
              Transfer Completed & Posted
            </span>
            <h2 class="text-3xl font-extrabold text-slate-900 dark:text-white mt-1">
              {{ receipt()?.amount | money }}
            </h2>
            <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
              Double-entry debits and credits balanced atomically
            </p>
          </div>

          <div class="p-6 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-800 space-y-3 text-xs">
            <div class="flex justify-between">
              <span class="text-slate-400">Transaction Reference:</span>
              <span class="font-mono font-bold text-slate-900 dark:text-white">{{ receipt()?.reference }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Timestamp:</span>
              <span>{{ receipt()?.timestamp | date: 'medium' }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Recipient:</span>
              <span class="font-semibold text-slate-900 dark:text-white">{{ receipt()?.destinationAccountHolder || beneficiary()?.accountHolderName }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Recipient Account:</span>
              <span class="font-mono">{{ receipt()?.destinationAccountNumber || form.value.destinationAccountNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Transfer Fee:</span>
              <span class="font-medium text-emerald-600">৳ 0.00 (Zero Fee)</span>
            </div>
            <div class="flex justify-between pt-2 border-t border-slate-200/60 dark:border-slate-700">
              <span class="text-slate-400">Remaining Balance:</span>
              <span class="font-bold text-slate-900 dark:text-white">{{ receipt()?.balanceAfter | money }}</span>
            </div>
          </div>

          <div class="flex flex-col sm:flex-row items-center gap-3">
            <button
              type="button"
              (click)="printReceipt()"
              class="w-full sm:flex-1 py-2.5 px-4 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors flex items-center justify-center gap-2"
            >
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
              </svg>
              <span>Print Official Receipt</span>
            </button>
            <button
              type="button"
              (click)="resetTransfer()"
              class="w-full sm:flex-1 py-2.5 px-4 rounded-xl text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 shadow-md transition-all"
            >
              Transfer Again
            </button>
          </div>
        </div>
      }

      <!-- Transfer Confirmation Modal -->
      <app-modal
        [isOpen]="showConfirmModal()"
        title="Confirm Intra-Bank Transfer"
        [showFooter]="true"
        (close)="showConfirmModal.set(false)"
      >
        <div class="space-y-4 text-xs text-slate-700 dark:text-slate-300">
          <p class="leading-relaxed">
            Please verify the beneficiary and transfer details before authorizing the double-entry ledger posting:
          </p>

          <div class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200/80 dark:border-slate-800 space-y-2.5">
            <div class="flex justify-between">
              <span class="text-slate-400">Debit Account:</span>
              <span class="font-mono font-medium">{{ selectedSource()?.accountNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Beneficiary Name:</span>
              <span class="font-bold text-slate-900 dark:text-white">{{ beneficiary()?.accountHolderName }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Beneficiary Account:</span>
              <span class="font-mono">{{ form.value.destinationAccountNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Transfer Amount:</span>
              <span class="font-bold text-slate-900 dark:text-white">{{ form.value.amount | money }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-400">Transfer Fee:</span>
              <span class="text-emerald-600 font-semibold">৳ 0.00</span>
            </div>
            <div class="flex justify-between pt-2 border-t border-slate-200/60 dark:border-slate-700 text-sm font-bold">
              <span>Total Debit:</span>
              <span class="text-sky-600">{{ form.value.amount | money }}</span>
            </div>
          </div>

          <div class="text-[11px] text-slate-400">
            Idempotency Key: <span class="font-mono text-slate-500">{{ idempotencyKey() }}</span>
          </div>
        </div>

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showConfirmModal.set(false)"
            class="px-3 py-1.5 rounded-xl text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="executeTransfer()"
            [disabled]="loading()"
            class="px-4 py-1.5 rounded-xl text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 disabled:opacity-50 shadow-md flex items-center gap-2"
          >
            @if (loading()) {
              <svg class="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Posting Transaction...</span>
            } @else {
              <span>Confirm & Send Funds</span>
            }
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class TransferComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private transactionService = inject(TransactionService);
  private idempotencyService = inject(IdempotencyService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);

  public accounts = signal<AccountDto[]>([]);
  public selectedSource = signal<AccountDto | null>(null);
  public beneficiary = signal<BeneficiaryLookupDto | null>(null);
  public beneficiaryError = signal<string | null>(null);
  public lookingUpBeneficiary = signal(false);
  public insufficientBalanceWarning = signal(false);
  public loading = signal(false);
  public showConfirmModal = signal(false);
  public receipt = signal<TransactionReceiptDto | null>(null);

  private readonly ACTION_TAG = 'customer_transfer_wizard';
  public idempotencyKey = signal('');

  public form = this.fb.group({
    sourceAccountId: ['', [Validators.required]],
    destinationAccountNumber: ['', [Validators.required, Validators.minLength(10)]],
    amount: ['', [Validators.required, Validators.min(1)]],
    referenceNote: ['Intra-bank fund transfer', [Validators.required]],
  });

  ngOnInit(): void {
    this.refreshIdempotencyKey();

    this.accountService.getAccounts().subscribe({
      next: (data) => {
        this.accounts.set(data || []);
        const paramSource = this.route.snapshot.queryParams['source'];
        if (paramSource && data.some((a) => a.id === paramSource)) {
          this.form.patchValue({ sourceAccountId: paramSource });
        } else if (data.length > 0) {
          this.form.patchValue({ sourceAccountId: data[0].id });
        }
        this.onSourceAccountChange();
      },
    });
  }

  public refreshIdempotencyKey(): void {
    const key = this.idempotencyService.refreshKey(this.ACTION_TAG);
    this.idempotencyKey.set(key);
  }

  public onSourceAccountChange(): void {
    const srcId = this.form.get('sourceAccountId')?.value;
    const found = this.accounts().find((a) => a.id === srcId) || null;
    this.selectedSource.set(found);
    this.checkAmountLimit();
  }

  public setQuickAmount(amt: number): void {
    this.form.patchValue({ amount: amt.toString() });
    this.checkAmountLimit();
  }

  public checkAmountLimit(): void {
    const src = this.selectedSource();
    const amtStr = this.form.get('amount')?.value;
    if (!src || !amtStr) {
      this.insufficientBalanceWarning.set(false);
      return;
    }

    const available = src.availableBalance || '0';
    const overdraft = src.overdraftLimit || '0';
    const totalLiquidity = MoneyUtil.add(available, overdraft);

    this.insufficientBalanceWarning.set(MoneyUtil.isGreaterThan(amtStr, totalLiquidity));
  }

  public onBeneficiaryInput(): void {
    const num = this.form.get('destinationAccountNumber')?.value?.trim();
    if (num && num.length >= 10) {
      this.lookupBeneficiary();
    } else {
      this.beneficiary.set(null);
      this.beneficiaryError.set(null);
    }
  }

  public lookupBeneficiary(): void {
    const num = this.form.get('destinationAccountNumber')?.value?.trim();
    if (!num) return;

    const src = this.selectedSource();
    if (src && src.accountNumber === num) {
      this.beneficiary.set(null);
      this.beneficiaryError.set('Cannot transfer funds to the same source account.');
      return;
    }

    this.lookingUpBeneficiary.set(true);
    this.beneficiaryError.set(null);

    this.accountService.lookupBeneficiary(num).subscribe({
      next: (ben) => {
        this.lookingUpBeneficiary.set(false);
        this.beneficiary.set(ben);
      },
      error: (err) => {
        this.lookingUpBeneficiary.set(false);
        this.beneficiary.set(null);
        this.beneficiaryError.set('Account not found or invalid account number.');
      },
    });
  }

  public openConfirmationModal(): void {
    if (this.form.invalid || this.insufficientBalanceWarning() || !this.beneficiary()) return;
    this.showConfirmModal.set(true);
  }

  public executeTransfer(): void {
    if (this.loading()) return;
    this.loading.set(true);

    const val = this.form.value;
    this.transactionService
      .transfer({
        sourceAccountId: val.sourceAccountId!,
        destinationAccountNumber: val.destinationAccountNumber!,
        amount: val.amount!,
        currency: 'BDT',
        referenceNote: val.referenceNote || 'Intra-bank transfer',
      })
      .subscribe({
        next: (receiptRes) => {
          this.loading.set(false);
          this.showConfirmModal.set(false);
          this.receipt.set(receiptRes);
          this.notification.success('Transfer Posted', `Reference: ${receiptRes.reference}`);
        },
        error: () => {
          this.loading.set(false);
          this.showConfirmModal.set(false);
        },
      });
  }

  public printReceipt(): void {
    window.print();
  }

  public resetTransfer(): void {
    this.receipt.set(null);
    this.beneficiary.set(null);
    this.refreshIdempotencyKey();
    this.form.patchValue({
      destinationAccountNumber: '',
      amount: '',
      referenceNote: 'Intra-bank fund transfer',
    });
  }
}
