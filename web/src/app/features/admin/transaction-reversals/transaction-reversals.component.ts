import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { IdempotencyService } from '../../../core/services/idempotency.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TransactionItemDto } from '../../../core/models/transaction.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-transaction-reversals',
  standalone: true,
  imports: [CommonModule, FormsModule, MoneyPipe, BadgeComponent, ModalComponent],
  template: `
    <div class="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Transaction Reversals</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Post compensating ledger transactions to correct errors. Original ledger entries remain strictly immutable.
        </p>
      </div>

      <!-- Financial Integrity Rules Card -->
      <div class="p-4 rounded-xl bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-xs text-slate-600 dark:text-slate-300 flex items-start gap-3">
        <div class="p-1.5 rounded-lg bg-sky-100 dark:bg-sky-950 text-sky-600 shrink-0 mt-0.5">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <div class="space-y-1">
          <span class="font-bold text-slate-900 dark:text-white">Financial Invariant Notice</span>
          <p class="leading-relaxed">
            In compliance with double-entry accounting standards, transactions are never updated or deleted. Reversing an entry executes an exact compensating journal with opposite debits and credits, preserving complete audit provenance.
          </p>
        </div>
      </div>

      <!-- Transactions List -->
      @if (loading()) {
        <div class="space-y-3">
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
        </div>
      } @else if (transactions().length === 0) {
        <div class="p-12 rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 text-center bg-white dark:bg-slate-900">
          <h3 class="font-semibold text-slate-900 dark:text-white">No transactions found</h3>
          <p class="text-xs text-slate-500 mt-1">No recorded financial postings in the ledger.</p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm text-slate-600 dark:text-slate-300">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-[11px] uppercase font-bold text-slate-400 border-b border-slate-200/70 dark:border-slate-800">
                <tr>
                  <th class="px-6 py-4">Reference</th>
                  <th class="px-6 py-4">Type</th>
                  <th class="px-6 py-4">Status & Compensation</th>
                  <th class="px-6 py-4">Timestamp</th>
                  <th class="px-6 py-4 text-right">Amount</th>
                  <th class="px-6 py-4 text-right">Action</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                @for (tx of transactions(); track tx.id) {
                  <tr class="hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors">
                    <td class="px-6 py-4 font-mono font-bold text-xs text-slate-900 dark:text-white">
                      {{ tx.reference }}
                    </td>
                    <td class="px-6 py-4">
                      <span class="text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                        {{ tx.type }}
                      </span>
                    </td>
                    <td class="px-6 py-4">
                      <div class="flex items-center gap-2">
                        <app-badge [variant]="tx.status === 'POSTED' ? 'success' : 'danger'">
                          {{ tx.status }}
                        </app-badge>
                        @if (tx.reversedByTransactionId) {
                          <span class="text-[10px] font-mono text-rose-500" title="Reversed by compensating transaction">
                            ↳ Compensated
                          </span>
                        }
                        @if (tx.reversesTransactionId) {
                          <span class="text-[10px] font-mono text-sky-500" title="Compensating reversal of prior transaction">
                            Compensating Leg
                          </span>
                        }
                      </div>
                    </td>
                    <td class="px-6 py-4 text-xs text-slate-500">
                      {{ tx.postedAt | date: 'medium' }}
                    </td>
                    <td class="px-6 py-4 text-right font-mono font-bold text-slate-900 dark:text-white">
                      {{ tx.amount | money }}
                    </td>
                    <td class="px-6 py-4 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          (click)="toggleExpand(tx.id)"
                          class="px-2.5 py-1 text-xs font-medium text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 rounded-lg transition-colors"
                        >
                          {{ expandedTxId() === tx.id ? 'Hide Lines' : 'Inspect Ledger' }}
                        </button>
                        @if (tx.status === 'POSTED' && !tx.reversedByTransactionId && tx.type !== 'REVERSAL') {
                          <button
                            type="button"
                            (click)="openReversalModal(tx)"
                            class="px-2.5 py-1 text-xs font-semibold text-rose-600 bg-rose-50 dark:bg-rose-950/60 hover:bg-rose-100 rounded-lg transition-colors"
                          >
                            Reverse
                          </button>
                        }
                      </div>
                    </td>
                  </tr>

                  <!-- Ledger Lines Breakdown -->
                  @if (expandedTxId() === tx.id) {
                    <tr class="bg-slate-50/90 dark:bg-slate-800/60">
                      <td colspan="6" class="px-6 py-4">
                        <div class="text-xs space-y-2">
                          <div class="font-bold uppercase tracking-wider text-slate-500 text-[10px]">
                            Underlying Journal Lines (Double-Entry Verification)
                          </div>
                          @if (tx.ledgerEntries && tx.ledgerEntries.length > 0) {
                            <div class="border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden bg-white dark:bg-slate-900 font-mono text-xs">
                              <table class="w-full">
                                <thead class="bg-slate-100 dark:bg-slate-800 text-[10px] text-slate-500 uppercase">
                                  <tr>
                                    <th class="px-3 py-1.5 text-left">Account</th>
                                    <th class="px-3 py-1.5 text-left">Direction</th>
                                    <th class="px-3 py-1.5 text-right">Debit</th>
                                    <th class="px-3 py-1.5 text-right">Credit</th>
                                    <th class="px-3 py-1.5 text-right">Seq</th>
                                  </tr>
                                </thead>
                                <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                                  @for (entry of tx.ledgerEntries; track entry.id) {
                                    <tr>
                                      <td class="px-3 py-1.5 font-sans">{{ entry.accountName || entry.accountNumber || entry.accountId }}</td>
                                      <td class="px-3 py-1.5">
                                        <span [ngClass]="entry.direction === 'DEBIT' ? 'text-rose-600 font-bold' : 'text-emerald-600 font-bold'">
                                          {{ entry.direction }}
                                        </span>
                                      </td>
                                      <td class="px-3 py-1.5 text-right">{{ entry.direction === 'DEBIT' ? (entry.amount | money) : '—' }}</td>
                                      <td class="px-3 py-1.5 text-right">{{ entry.direction === 'CREDIT' ? (entry.amount | money) : '—' }}</td>
                                      <td class="px-3 py-1.5 text-right text-slate-400">#{{ entry.accountSeq }}</td>
                                    </tr>
                                  }
                                </tbody>
                              </table>
                            </div>
                          } @else {
                            <div class="text-slate-400 italic">Journal lines stored in immutable ledger.</div>
                          }
                        </div>
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Reversal Modal Dialog -->
      <app-modal
        [isOpen]="showModal()"
        title="Admin Transaction Reversal"
        [showFooter]="true"
        (close)="showModal.set(false)"
      >
        <div class="space-y-4 text-xs text-slate-600 dark:text-slate-300">
          <p>
            You are creating a compensating reversal for transaction
            <strong class="font-mono">{{ targetTx()?.reference }}</strong> (Amount: {{ targetTx()?.amount | money }}).
          </p>

          <div class="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 text-amber-900 dark:text-amber-200">
            ⚠️ <strong>Notice:</strong> This will create a linked compensating transaction in the database that mirrors and inverts the debit/credit lines.
          </div>

          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">
              Required Reversal Reason
            </label>
            <textarea
              [(ngModel)]="reversalReason"
              rows="3"
              placeholder="e.g. Incorrect counterparty account entered / Teller entry error / Authorized dispute"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-rose-500"
            ></textarea>
          </div>
        </div>

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showModal.set(false)"
            class="px-3 py-1.5 rounded-xl text-xs font-medium text-slate-600 hover:bg-slate-200"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="confirmReversal()"
            [disabled]="!reversalReason.trim() || processing()"
            class="px-4 py-1.5 rounded-xl text-xs font-semibold text-white bg-rose-600 hover:bg-rose-500 disabled:opacity-50"
          >
            {{ processing() ? 'Posting Reversal...' : 'Post Compensating Transaction' }}
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class TransactionReversalsComponent implements OnInit {
  private adminService = inject(AdminService);
  private idempotencyService = inject(IdempotencyService);
  private notification = inject(NotificationService);

  public transactions = signal<TransactionItemDto[]>([]);
  public loading = signal(true);
  public expandedTxId = signal<string | null>(null);
  public showModal = signal(false);
  public targetTx = signal<TransactionItemDto | null>(null);
  public reversalReason = '';
  public processing = signal(false);

  private readonly ACTION_TAG = 'admin_reversal_dialog';

  ngOnInit(): void {
    this.loadTransactions();
  }

  public loadTransactions(): void {
    this.loading.set(true);
    this.adminService.getTransactions({ limit: 50 }).subscribe({
      next: (res) => {
        this.transactions.set(res.items || []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  public toggleExpand(id: string): void {
    this.expandedTxId.update((curr) => (curr === id ? null : id));
  }

  public openReversalModal(tx: TransactionItemDto): void {
    this.targetTx.set(tx);
    this.reversalReason = '';
    this.idempotencyService.refreshKey(this.ACTION_TAG);
    this.showModal.set(true);
  }

  public confirmReversal(): void {
    const tx = this.targetTx();
    if (!tx || !this.reversalReason.trim() || this.processing()) return;

    this.processing.set(true);
    this.adminService.reverseTransaction(tx.id, this.reversalReason).subscribe({
      next: (res) => {
        this.processing.set(false);
        this.showModal.set(false);
        this.notification.success('Reversal Completed', `Compensating Ref: ${res.reference}`);
        this.loadTransactions();
      },
      error: () => this.processing.set(false),
    });
  }
}
