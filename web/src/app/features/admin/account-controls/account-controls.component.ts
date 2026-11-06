import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { AccountService } from '../../../core/services/account.service';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AccountDto } from '../../../core/models/account.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-account-controls',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, MoneyPipe, BadgeComponent, ModalComponent],
  template: `
    <div class="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Account Controls & Governance</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Enforce immediate account freezes (blocking all debits), unfreezing, and administrative closures
        </p>
      </div>

      <!-- Accounts Table -->
      @if (loading()) {
        <div class="space-y-3">
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm text-slate-600 dark:text-slate-300">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-[11px] uppercase font-bold text-slate-400 border-b border-slate-200/70 dark:border-slate-800">
                <tr>
                  <th class="px-6 py-4">Account Number</th>
                  <th class="px-6 py-4">Type</th>
                  <th class="px-6 py-4">Status</th>
                  <th class="px-6 py-4 text-right">Available Balance</th>
                  <th class="px-6 py-4 text-right">Ledger Balance</th>
                  <th class="px-6 py-4 text-right">Administrative Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                @for (acc of accounts(); track acc.id) {
                  <tr class="hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors">
                    <td class="px-6 py-4 font-mono font-bold text-slate-900 dark:text-white">
                      {{ acc.accountNumber }}
                    </td>
                    <td class="px-6 py-4">
                      <span class="text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800">
                        {{ acc.accountType }}
                      </span>
                    </td>
                    <td class="px-6 py-4">
                      <app-badge [variant]="acc.status === 'ACTIVE' ? 'success' : acc.status === 'FROZEN' ? 'warning' : 'danger'">
                        {{ acc.status }}
                      </app-badge>
                    </td>
                    <td class="px-6 py-4 text-right font-bold text-slate-900 dark:text-white">
                      {{ acc.availableBalance | money }}
                    </td>
                    <td class="px-6 py-4 text-right text-xs text-slate-500">
                      {{ acc.ledgerBalance | money }}
                    </td>
                    <td class="px-6 py-4 text-right">
                      <div class="flex items-center justify-end gap-2">
                        @if (acc.status === 'ACTIVE') {
                          <button
                            type="button"
                            (click)="openFreezeModal(acc)"
                            class="px-2.5 py-1 text-xs font-semibold text-amber-700 bg-amber-50 dark:bg-amber-950/60 hover:bg-amber-100 rounded-lg transition-colors"
                          >
                            Freeze Account
                          </button>
                        } @else if (acc.status === 'FROZEN') {
                          <button
                            type="button"
                            (click)="unfreezeAccount(acc)"
                            class="px-2.5 py-1 text-xs font-semibold text-emerald-700 bg-emerald-50 dark:bg-emerald-950/60 hover:bg-emerald-100 rounded-lg transition-colors"
                          >
                            Unfreeze Account
                          </button>
                        }

                        @if (acc.status !== 'CLOSED') {
                          <button
                            type="button"
                            (click)="openCloseModal(acc)"
                            class="px-2.5 py-1 text-xs font-semibold text-rose-600 bg-rose-50 dark:bg-rose-950/60 hover:bg-rose-100 rounded-lg transition-colors"
                          >
                            Close
                          </button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Freeze Modal -->
      <app-modal
        [isOpen]="showFreezeModal()"
        title="Freeze Account (Block Debits)"
        [showFooter]="true"
        (close)="showFreezeModal.set(false)"
      >
        <div class="space-y-4 text-xs text-slate-600 dark:text-slate-300">
          <p>
            Freezing account <strong class="font-mono">{{ targetAccount()?.accountNumber }}</strong> will
            prevent all outward transfers and withdrawals immediately.
          </p>

          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">
              Freeze Reason (Recorded in Immutable Audit Log)
            </label>
            <textarea
              [(ngModel)]="freezeReason"
              rows="3"
              placeholder="e.g. Unusual transfer velocity detected / Court order freeze / Suspected fraud"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-amber-500"
            ></textarea>
          </div>
        </div>

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showFreezeModal.set(false)"
            class="px-3 py-1.5 rounded-xl text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-200"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="confirmFreeze()"
            [disabled]="!freezeReason.trim() || processingAction()"
            class="px-4 py-1.5 rounded-xl text-xs font-semibold text-white bg-amber-600 hover:bg-amber-500 disabled:opacity-50"
          >
            {{ processingAction() ? 'Freezing...' : 'Confirm Freeze' }}
          </button>
        </div>
      </app-modal>

      <!-- Close Modal -->
      <app-modal
        [isOpen]="showCloseModal()"
        title="Administrative Account Closure"
        [showFooter]="true"
        (close)="showCloseModal.set(false)"
      >
        <div class="space-y-3 text-xs text-slate-600 dark:text-slate-300">
          <p>
            Confirm permanent closure of account <strong class="font-mono">{{ targetAccount()?.accountNumber }}</strong>.
          </p>
          <div class="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 text-amber-900 dark:text-amber-200">
            Account must have 0.00 BDT balance to be closed.
          </div>
        </div>

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showCloseModal.set(false)"
            class="px-3 py-1.5 rounded-xl text-xs font-medium text-slate-600 hover:bg-slate-200"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="confirmClose()"
            [disabled]="processingAction()"
            class="px-4 py-1.5 rounded-xl text-xs font-semibold text-white bg-rose-600 hover:bg-rose-500 disabled:opacity-50"
          >
            {{ processingAction() ? 'Closing...' : 'Close Account' }}
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class AccountControlsComponent implements OnInit {
  private accountService = inject(AccountService);
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);

  public accounts = signal<AccountDto[]>([]);
  public loading = signal(true);
  public showFreezeModal = signal(false);
  public showCloseModal = signal(false);
  public targetAccount = signal<AccountDto | null>(null);
  public freezeReason = '';
  public processingAction = signal(false);

  ngOnInit(): void {
    this.loadAccounts();
  }

  public loadAccounts(): void {
    this.loading.set(true);
    this.accountService.getAccounts().subscribe({
      next: (res) => {
        this.accounts.set(res || []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  public openFreezeModal(acc: AccountDto): void {
    this.targetAccount.set(acc);
    this.freezeReason = '';
    this.showFreezeModal.set(true);
  }

  public confirmFreeze(): void {
    const acc = this.targetAccount();
    if (!acc || !this.freezeReason.trim() || this.processingAction()) return;

    this.processingAction.set(true);
    this.adminService.freezeAccount(acc.id, this.freezeReason).subscribe({
      next: () => {
        this.processingAction.set(false);
        this.showFreezeModal.set(false);
        this.notification.success('Account Frozen', `Account ${acc.accountNumber} has been frozen.`);
        this.loadAccounts();
      },
      error: () => this.processingAction.set(false),
    });
  }

  public unfreezeAccount(acc: AccountDto): void {
    this.adminService.unfreezeAccount(acc.id).subscribe({
      next: () => {
        this.notification.success('Account Unfrozen', `Account ${acc.accountNumber} has been reactivated.`);
        this.loadAccounts();
      },
    });
  }

  public openCloseModal(acc: AccountDto): void {
    this.targetAccount.set(acc);
    this.showCloseModal.set(true);
  }

  public confirmClose(): void {
    const acc = this.targetAccount();
    if (!acc || this.processingAction()) return;

    this.processingAction.set(true);
    this.accountService.closeAccount(acc.id).subscribe({
      next: () => {
        this.processingAction.set(false);
        this.showCloseModal.set(false);
        this.notification.success('Account Closed', `Account ${acc.accountNumber} has been closed.`);
        this.loadAccounts();
      },
      error: () => this.processingAction.set(false),
    });
  }
}
