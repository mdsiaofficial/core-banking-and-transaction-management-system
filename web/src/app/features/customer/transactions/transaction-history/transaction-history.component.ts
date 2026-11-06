import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TransactionService, TransactionFilterParams } from '../../../../core/services/transaction.service';
import { AccountService } from '../../../../core/services/account.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { AccountDto } from '../../../../core/models/account.models';
import { TransactionItemDto } from '../../../../core/models/transaction.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-transaction-history',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MoneyPipe, BadgeComponent],
  template: `
    <div class="space-y-6 max-w-7xl mx-auto">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Transaction History</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
            Immutable double-entry ledger activity with cursor-based pagination
          </p>
        </div>

        <button
          type="button"
          (click)="exportCsv()"
          [disabled]="exporting() || !selectedAccountId()"
          class="px-4 py-2 rounded-xl text-xs font-semibold text-white bg-slate-900 dark:bg-slate-700 hover:bg-slate-800 transition-colors shadow-sm flex items-center gap-2 self-start sm:self-auto"
        >
          @if (exporting()) {
            <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <span>Exporting CSV...</span>
          } @else {
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <span>Download Statement (CSV)</span>
          }
        </button>
      </div>

      <!-- Filter Controls Bar -->
      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm">
        <form [formGroup]="filterForm" (ngSubmit)="applyFilters()" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
          <!-- Account Selector -->
          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">Account</label>
            <select
              formControlName="accountId"
              (change)="onAccountChange()"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            >
              @for (acc of accounts(); track acc.id) {
                <option [value]="acc.id">
                  {{ acc.accountNumber }} ({{ acc.accountType }})
                </option>
              }
            </select>
          </div>

          <!-- Direction Filter -->
          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">Direction</label>
            <select
              formControlName="direction"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            >
              <option value="">All Directions</option>
              <option value="CREDIT">Credits (+) Inflow</option>
              <option value="DEBIT">Debits (-) Outflow</option>
            </select>
          </div>

          <!-- Type Filter -->
          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">Type</label>
            <select
              formControlName="type"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            >
              <option value="">All Types</option>
              <option value="TRANSFER">Transfer</option>
              <option value="DEPOSIT">Deposit</option>
              <option value="WITHDRAWAL">Withdrawal</option>
              <option value="REVERSAL">Reversal</option>
            </select>
          </div>

          <!-- Date From -->
          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">From Date</label>
            <input
              type="date"
              formControlName="from"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            />
          </div>

          <!-- Actions -->
          <div class="flex items-end gap-2">
            <button
              type="submit"
              class="flex-1 py-2 px-3 rounded-xl text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 shadow-sm transition-all"
            >
              Apply Filter
            </button>
            <button
              type="button"
              (click)="resetFilters()"
              class="py-2 px-3 rounded-xl text-xs font-medium text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
            >
              Reset
            </button>
          </div>
        </form>
      </div>

      <!-- Transactions Table -->
      @if (loading()) {
        <div class="space-y-3">
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
        </div>
      } @else if (transactions().length === 0) {
        <div class="p-12 rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 text-center bg-white dark:bg-slate-900">
          <div class="w-12 h-12 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-400 mx-auto flex items-center justify-center mb-3">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <h3 class="font-semibold text-slate-900 dark:text-white">No transactions found</h3>
          <p class="text-xs text-slate-500 mt-1">
            No entries match your selected account and filters.
          </p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm text-slate-600 dark:text-slate-300">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-[11px] uppercase font-bold text-slate-400 border-b border-slate-200/70 dark:border-slate-800">
                <tr>
                  <th class="px-6 py-4">Reference</th>
                  <th class="px-6 py-4">Type & Details</th>
                  <th class="px-6 py-4">Timestamp</th>
                  <th class="px-6 py-4 text-right">Amount</th>
                  <th class="px-6 py-4 text-center">Ledger Lines</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                @for (tx of transactions(); track tx.id) {
                  <tr class="hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors">
                    <td class="px-6 py-4 font-mono text-xs font-semibold text-slate-900 dark:text-white">
                      {{ tx.reference }}
                    </td>
                    <td class="px-6 py-4">
                      <div class="flex items-center gap-2">
                        <app-badge [variant]="tx.direction === 'CREDIT' ? 'success' : 'danger'">
                          {{ tx.direction }}
                        </app-badge>
                        <span class="text-xs font-medium text-slate-700 dark:text-slate-200">
                          {{ tx.type }}
                        </span>
                      </div>
                      <div class="text-[11px] text-slate-400 mt-0.5 max-w-xs truncate">
                        {{ tx.description }}
                      </div>
                    </td>
                    <td class="px-6 py-4 text-xs text-slate-500 dark:text-slate-400">
                      {{ tx.postedAt | date: 'medium' }}
                    </td>
                    <td class="px-6 py-4 text-right">
                      <span
                        class="font-mono font-bold text-sm"
                        [ngClass]="tx.direction === 'CREDIT' ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'"
                      >
                        {{ tx.direction === 'CREDIT' ? '+' : '-' }}{{ tx.amount | money }}
                      </span>
                    </td>
                    <td class="px-6 py-4 text-center">
                      <button
                        type="button"
                        (click)="toggleExpand(tx.id)"
                        class="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
                        title="Toggle ledger entries breakdown"
                      >
                        <svg
                          class="w-4 h-4 transition-transform"
                          [class.rotate-180]="expandedTxId() === tx.id"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                        >
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                        </svg>
                      </button>
                    </td>
                  </tr>

                  <!-- Expandable Double-Entry Ledger Lines -->
                  @if (expandedTxId() === tx.id) {
                    <tr class="bg-slate-50/90 dark:bg-slate-800/60">
                      <td colspan="5" class="px-6 py-4">
                        <div class="text-xs space-y-2">
                          <div class="font-bold uppercase tracking-wider text-slate-500 text-[10px]">
                            Double-Entry Posting Ledger Journal (Balanced Σ Debits = Σ Credits)
                          </div>
                          @if (tx.ledgerEntries && tx.ledgerEntries.length > 0) {
                            <div class="border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden bg-white dark:bg-slate-900">
                              <table class="w-full text-xs">
                                <thead class="bg-slate-100 dark:bg-slate-800 text-[10px] text-slate-500 uppercase font-semibold">
                                  <tr>
                                    <th class="px-3 py-2 text-left">Account</th>
                                    <th class="px-3 py-2 text-left">Direction</th>
                                    <th class="px-3 py-2 text-right">Debit</th>
                                    <th class="px-3 py-2 text-right">Credit</th>
                                    <th class="px-3 py-2 text-right">Balance After</th>
                                  </tr>
                                </thead>
                                <tbody class="divide-y divide-slate-100 dark:divide-slate-800 font-mono">
                                  @for (entry of tx.ledgerEntries; track entry.id) {
                                    <tr>
                                      <td class="px-3 py-2 font-sans font-medium text-slate-900 dark:text-white">
                                        {{ entry.accountName || entry.accountNumber || entry.accountId }}
                                      </td>
                                      <td class="px-3 py-2">
                                        <span [ngClass]="entry.direction === 'DEBIT' ? 'text-rose-600' : 'text-emerald-600'" class="font-bold">
                                          {{ entry.direction }}
                                        </span>
                                      </td>
                                      <td class="px-3 py-2 text-right">
                                        {{ entry.direction === 'DEBIT' ? (entry.amount | money) : '—' }}
                                      </td>
                                      <td class="px-3 py-2 text-right">
                                        {{ entry.direction === 'CREDIT' ? (entry.amount | money) : '—' }}
                                      </td>
                                      <td class="px-3 py-2 text-right font-semibold">
                                        {{ entry.balanceAfter | money }}
                                      </td>
                                    </tr>
                                  }
                                </tbody>
                              </table>
                            </div>
                          } @else {
                            <div class="text-slate-400 italic">
                              Ledger lines recorded in core database journal.
                            </div>
                          }
                        </div>
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          </div>

          <!-- Keyset Cursor Pagination -->
          @if (nextCursor()) {
            <div class="p-4 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-100 dark:border-slate-800 text-center">
              <button
                type="button"
                (click)="loadMore()"
                [disabled]="loadingMore()"
                class="px-4 py-2 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 hover:bg-slate-50 transition-all shadow-sm"
              >
                {{ loadingMore() ? 'Loading More Records...' : 'Load More Transactions' }}
              </button>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class TransactionHistoryComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private transactionService = inject(TransactionService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);

  public accounts = signal<AccountDto[]>([]);
  public transactions = signal<TransactionItemDto[]>([]);
  public loading = signal(true);
  public loadingMore = signal(false);
  public exporting = signal(false);
  public nextCursor = signal<string | null>(null);
  public expandedTxId = signal<string | null>(null);

  public selectedAccountId = signal<string>('');

  public filterForm = this.fb.group({
    accountId: [''],
    direction: [''],
    type: [''],
    from: [''],
    to: [''],
  });

  ngOnInit(): void {
    this.accountService.getAccounts().subscribe({
      next: (accs) => {
        this.accounts.set(accs || []);
        const paramAcc = this.route.snapshot.queryParams['account'];
        if (paramAcc && accs.some((a) => a.id === paramAcc)) {
          this.filterForm.patchValue({ accountId: paramAcc });
          this.selectedAccountId.set(paramAcc);
        } else if (accs.length > 0) {
          this.filterForm.patchValue({ accountId: accs[0].id });
          this.selectedAccountId.set(accs[0].id);
        }
        this.fetchTransactions();
      },
    });
  }

  public onAccountChange(): void {
    const accId = this.filterForm.get('accountId')?.value;
    if (accId) {
      this.selectedAccountId.set(accId);
      this.fetchTransactions();
    }
  }

  public applyFilters(): void {
    this.fetchTransactions();
  }

  public resetFilters(): void {
    const currentAcc = this.selectedAccountId();
    this.filterForm.reset({
      accountId: currentAcc,
      direction: '',
      type: '',
      from: '',
      to: '',
    });
    this.fetchTransactions();
  }

  public toggleExpand(id: string): void {
    this.expandedTxId.update((curr) => (curr === id ? null : id));
  }

  public fetchTransactions(): void {
    const accId = this.filterForm.get('accountId')?.value;
    if (!accId) return;

    this.loading.set(true);
    const val = this.filterForm.value;

    const params: TransactionFilterParams = {
      limit: 20,
      direction: (val.direction as any) || undefined,
      type: val.type || undefined,
      from: val.from || undefined,
      to: val.to || undefined,
    };

    this.transactionService.getAccountTransactions(accId, params).subscribe({
      next: (res) => {
        this.transactions.set(res.items || []);
        this.nextCursor.set(res.nextCursor);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  public loadMore(): void {
    const cursor = this.nextCursor();
    const accId = this.selectedAccountId();
    if (!cursor || !accId || this.loadingMore()) return;

    this.loadingMore.set(true);
    const val = this.filterForm.value;

    this.transactionService
      .getAccountTransactions(accId, {
        cursor,
        limit: 20,
        direction: (val.direction as any) || undefined,
        type: val.type || undefined,
        from: val.from || undefined,
        to: val.to || undefined,
      })
      .subscribe({
        next: (res) => {
          this.transactions.update((curr) => [...curr, ...(res.items || [])]);
          this.nextCursor.set(res.nextCursor);
          this.loadingMore.set(false);
        },
        error: () => this.loadingMore.set(false),
      });
  }

  public exportCsv(): void {
    const accId = this.selectedAccountId();
    if (!accId || this.exporting()) return;

    this.exporting.set(true);
    const { from, to } = this.filterForm.value;

    this.accountService
      .downloadStatementCsv(accId, from || undefined, to || undefined)
      .subscribe({
        next: (blob) => {
          this.exporting.set(false);
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Statement_${accId}.csv`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.notification.success('Statement Exported', 'CSV statement file downloaded.');
        },
        error: () => this.exporting.set(false),
      });
  }
}
