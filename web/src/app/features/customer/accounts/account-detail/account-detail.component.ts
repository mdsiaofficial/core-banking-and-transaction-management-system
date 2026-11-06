import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AccountService } from '../../../../core/services/account.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { AccountDto } from '../../../../core/models/account.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-account-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, MoneyPipe, BadgeComponent, ModalComponent],
  template: `
    <div class="space-y-6 max-w-5xl mx-auto">
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
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Account Details</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Account ID: <span class="font-mono">{{ accountId }}</span>
          </p>
        </div>
      </div>

      @if (loading()) {
        <div class="h-64 rounded-2xl bg-slate-200 dark:bg-slate-800 animate-pulse"></div>
      } @else if (account(); as acc) {
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <!-- Main Card -->
          <div class="md:col-span-2 p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm space-y-6">
            <div class="flex items-center justify-between">
              <div>
                <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Account Number</span>
                <div class="font-mono text-xl font-bold text-slate-900 dark:text-white mt-0.5">
                  {{ acc.accountNumber }}
                </div>
              </div>
              <app-badge [variant]="acc.status === 'ACTIVE' ? 'success' : acc.status === 'FROZEN' ? 'warning' : 'danger'">
                {{ acc.status }}
              </app-badge>
            </div>

            <!-- Balances -->
            <div class="grid grid-cols-2 gap-4 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-800">
              <div>
                <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">Available Balance</div>
                <div class="text-2xl font-extrabold text-slate-900 dark:text-white mt-1">
                  {{ acc.availableBalance | money }}
                </div>
              </div>
              <div>
                <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">Ledger Balance</div>
                <div class="text-2xl font-extrabold text-slate-700 dark:text-slate-300 mt-1">
                  {{ acc.ledgerBalance | money }}
                </div>
              </div>
            </div>

            <!-- Details List -->
            <div class="grid grid-cols-2 gap-y-4 text-xs">
              <div>
                <span class="text-slate-400 block">Account Type</span>
                <span class="font-semibold text-slate-800 dark:text-slate-200">{{ acc.accountType }}</span>
              </div>
              <div>
                <span class="text-slate-400 block">Currency</span>
                <span class="font-semibold text-slate-800 dark:text-slate-200">{{ acc.currency }}</span>
              </div>
              <div>
                <span class="text-slate-400 block">Overdraft Limit</span>
                <span class="font-semibold text-slate-800 dark:text-slate-200">{{ acc.overdraftLimit | money }}</span>
              </div>
              <div>
                <span class="text-slate-400 block">Minimum Balance</span>
                <span class="font-semibold text-slate-800 dark:text-slate-200">{{ acc.minimumBalance | money }}</span>
              </div>
              <div>
                <span class="text-slate-400 block">Created At</span>
                <span class="font-semibold text-slate-800 dark:text-slate-200">{{ acc.createdAt | date: 'medium' }}</span>
              </div>
              <div>
                <span class="text-slate-400 block">Last Active</span>
                <span class="font-semibold text-slate-800 dark:text-slate-200">{{ acc.updatedAt | date: 'medium' }}</span>
              </div>
            </div>

            <!-- Action Buttons -->
            <div class="pt-4 border-t border-slate-100 dark:border-slate-800 flex flex-wrap gap-3">
              <a
                [routerLink]="['/customer/payments/transfer']"
                [queryParams]="{ source: acc.id }"
                class="px-4 py-2 rounded-xl text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 shadow-sm transition-all"
              >
                Send Money
              </a>
              <a
                [routerLink]="['/customer/payments/deposit']"
                [queryParams]="{ account: acc.id }"
                class="px-4 py-2 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all"
              >
                Cash Deposit
              </a>
              <a
                [routerLink]="['/customer/payments/withdraw']"
                [queryParams]="{ account: acc.id }"
                class="px-4 py-2 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all"
              >
                Withdraw
              </a>
              @if (acc.status === 'ACTIVE') {
                <button
                  type="button"
                  (click)="showCloseModal.set(true)"
                  class="ml-auto px-3 py-2 rounded-xl text-xs font-semibold text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition-colors"
                >
                  Close Account
                </button>
              }
            </div>
          </div>

          <!-- Statement Download Panel -->
          <div class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm space-y-4 flex flex-col justify-between">
            <div>
              <div class="w-10 h-10 rounded-xl bg-indigo-50 dark:bg-indigo-950/60 text-indigo-600 dark:text-indigo-400 flex items-center justify-center mb-3">
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <h3 class="text-sm font-bold text-slate-900 dark:text-white">Export Statement</h3>
              <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
                Download verified financial transaction history in CSV format.
              </p>

              <form [formGroup]="statementForm" class="mt-4 space-y-3">
                <div>
                  <label class="block text-[11px] font-semibold uppercase tracking-wider text-slate-500 mb-1">
                    From Date
                  </label>
                  <input
                    type="date"
                    formControlName="fromDate"
                    class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
                  />
                </div>
                <div>
                  <label class="block text-[11px] font-semibold uppercase tracking-wider text-slate-500 mb-1">
                    To Date
                  </label>
                  <input
                    type="date"
                    formControlName="toDate"
                    class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
                  />
                </div>
              </form>
            </div>

            <button
              type="button"
              (click)="downloadStatement()"
              [disabled]="exporting()"
              class="w-full py-2.5 px-3 rounded-xl text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-500 active:bg-indigo-700 shadow-md shadow-indigo-600/20 disabled:opacity-50 transition-all flex items-center justify-center gap-2"
            >
              @if (exporting()) {
                <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>Generating CSV...</span>
              } @else {
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                </svg>
                <span>Download CSV Statement</span>
              }
            </button>
          </div>
        </div>
      }

      <!-- Close Account Modal -->
      <app-modal
        [isOpen]="showCloseModal()"
        title="Close Bank Account"
        (close)="showCloseModal.set(false)"
      >
        <div class="space-y-4 text-xs text-slate-600 dark:text-slate-300">
          <p>
            Are you sure you want to request closure of account
            <strong class="font-mono">{{ account()?.accountNumber }}</strong>?
          </p>
          <div class="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-900/40 text-amber-900 dark:text-amber-200">
            ⚠️ <strong>Rule:</strong> The account balance must be exactly ৳0.00 before closing. If funds remain, please transfer or withdraw them first.
          </div>
        </div>

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showCloseModal.set(false)"
            class="px-3 py-1.5 rounded-xl text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="closeAccount()"
            [disabled]="closing()"
            class="px-4 py-1.5 rounded-xl text-xs font-semibold text-white bg-rose-600 hover:bg-rose-500 disabled:opacity-50 shadow-sm"
          >
            {{ closing() ? 'Processing...' : 'Confirm Account Closure' }}
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class AccountDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private notification = inject(NotificationService);

  public accountId = '';
  public account = signal<AccountDto | null>(null);
  public loading = signal(true);
  public exporting = signal(false);
  public closing = signal(false);
  public showCloseModal = signal(false);

  public statementForm = this.fb.group({
    fromDate: [''],
    toDate: [''],
  });

  ngOnInit(): void {
    this.accountId = this.route.snapshot.params['id'];
    this.loadAccount();
  }

  private loadAccount(): void {
    this.loading.set(true);
    this.accountService.getAccountById(this.accountId).subscribe({
      next: (acc) => {
        this.account.set(acc);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  public downloadStatement(): void {
    if (this.exporting()) return;
    this.exporting.set(true);

    const { fromDate, toDate } = this.statementForm.value;
    this.accountService
      .downloadStatementCsv(this.accountId, fromDate || undefined, toDate || undefined)
      .subscribe({
        next: (blob) => {
          this.exporting.set(false);
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Statement_${this.account()?.accountNumber || this.accountId}.csv`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.notification.success('Statement Exported', 'CSV statement downloaded successfully.');
        },
        error: () => this.exporting.set(false),
      });
  }

  public closeAccount(): void {
    this.closing.set(true);
    this.accountService.closeAccount(this.accountId).subscribe({
      next: () => {
        this.closing.set(false);
        this.showCloseModal.set(false);
        this.notification.success('Account Closed', 'The bank account has been successfully closed.');
        this.router.navigate(['/customer/accounts']);
      },
      error: () => this.closing.set(false),
    });
  }
}
