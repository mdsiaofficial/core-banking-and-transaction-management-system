import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerSummaryDto } from '../../../core/models/admin.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-customer-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink, MoneyPipe, BadgeComponent, ModalComponent],
  template: `
    <div class="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Customer Registry & Management</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Inspect registered identities, verify KYC status, and enforce account suspensions
        </p>
      </div>

      <!-- Search & Filters -->
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm">
        <form [formGroup]="searchForm" (ngSubmit)="applySearch()" class="flex flex-col sm:flex-row gap-3">
          <div class="flex-1 relative">
            <input
              type="text"
              formControlName="search"
              placeholder="Search by customer name or email..."
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            />
          </div>

          <div class="w-full sm:w-48">
            <select
              formControlName="status"
              (change)="applySearch()"
              class="w-full px-3 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            >
              <option value="">All Statuses</option>
              <option value="ACTIVE">ACTIVE</option>
              <option value="SUSPENDED">SUSPENDED</option>
              <option value="PENDING_VERIFICATION">PENDING_VERIFICATION</option>
            </select>
          </div>

          <button
            type="submit"
            class="py-2.5 px-4 rounded-xl text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 transition-colors shadow-sm"
          >
            Search
          </button>
        </form>
      </div>

      <!-- Customer Table -->
      @if (loading()) {
        <div class="space-y-3">
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
        </div>
      } @else if (customers().length === 0) {
        <div class="p-12 rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 text-center bg-white dark:bg-slate-900">
          <h3 class="font-semibold text-slate-900 dark:text-white">No customers found</h3>
          <p class="text-xs text-slate-500 mt-1">Try refining your search query.</p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm text-slate-600 dark:text-slate-300">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-[11px] uppercase font-bold text-slate-400 border-b border-slate-200/70 dark:border-slate-800">
                <tr>
                  <th class="px-6 py-4">Customer</th>
                  <th class="px-6 py-4">Status</th>
                  <th class="px-6 py-4">Phone / Contact</th>
                  <th class="px-6 py-4">Accounts</th>
                  <th class="px-6 py-4 text-right">Total Balance</th>
                  <th class="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                @for (c of customers(); track c.id) {
                  <tr class="hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors">
                    <td class="px-6 py-4">
                      <div class="font-bold text-slate-900 dark:text-white">{{ c.fullName }}</div>
                      <div class="text-xs text-slate-400 font-mono">{{ c.email }}</div>
                    </td>
                    <td class="px-6 py-4">
                      <app-badge [variant]="c.status === 'ACTIVE' ? 'success' : c.status === 'SUSPENDED' ? 'danger' : 'warning'">
                        {{ c.status }}
                      </app-badge>
                    </td>
                    <td class="px-6 py-4 text-xs font-mono text-slate-700 dark:text-slate-300">
                      {{ c.phone || '—' }}
                    </td>
                    <td class="px-6 py-4 text-xs font-semibold">
                      {{ c.accountsCount || 0 }} accounts
                    </td>
                    <td class="px-6 py-4 text-right font-bold text-slate-900 dark:text-white">
                      {{ c.totalBalanceBDT | money }}
                    </td>
                    <td class="px-6 py-4 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <a
                          [routerLink]="['/admin/customers', c.id]"
                          class="px-2.5 py-1 text-xs font-semibold text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 rounded-lg transition-colors"
                        >
                          View Profile
                        </a>
                        @if (c.status === 'ACTIVE') {
                          <button
                            type="button"
                            (click)="openSuspendModal(c)"
                            class="px-2.5 py-1 text-xs font-semibold text-rose-600 bg-rose-50 dark:bg-rose-950/60 hover:bg-rose-100 rounded-lg transition-colors"
                          >
                            Suspend
                          </button>
                        } @else if (c.status === 'SUSPENDED') {
                          <button
                            type="button"
                            (click)="reactivateCustomer(c)"
                            class="px-2.5 py-1 text-xs font-semibold text-emerald-600 bg-emerald-50 dark:bg-emerald-950/60 hover:bg-emerald-100 rounded-lg transition-colors"
                          >
                            Reactivate
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

      <!-- Suspend Modal -->
      <app-modal
        [isOpen]="showSuspendModal()"
        title="Suspend Customer Account"
        [showFooter]="true"
        (close)="showSuspendModal.set(false)"
      >
        <div class="space-y-4 text-xs text-slate-600 dark:text-slate-300">
          <p>
            Please provide an official audit reason for suspending customer
            <strong>{{ targetCustomer()?.fullName }}</strong>:
          </p>

          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">
              Suspension Reason (Required for Audit Trail)
            </label>
            <textarea
              [(ngModel)]="suspendReason"
              rows="3"
              placeholder="e.g. Suspected unauthorized account takeover / KYC verification document update required"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-rose-500"
            ></textarea>
          </div>
        </div>

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showSuspendModal.set(false)"
            class="px-3 py-1.5 rounded-xl text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-200"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="confirmSuspend()"
            [disabled]="!suspendReason.trim() || suspending()"
            class="px-4 py-1.5 rounded-xl text-xs font-semibold text-white bg-rose-600 hover:bg-rose-500 disabled:opacity-50"
          >
            {{ suspending() ? 'Suspending...' : 'Confirm Suspension' }}
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class CustomerManagementComponent implements OnInit {
  private fb = inject(FormBuilder);
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);

  public customers = signal<CustomerSummaryDto[]>([]);
  public loading = signal(true);
  public showSuspendModal = signal(false);
  public targetCustomer = signal<CustomerSummaryDto | null>(null);
  public suspendReason = '';
  public suspending = signal(false);

  public searchForm = this.fb.group({
    search: [''],
    status: [''],
  });

  ngOnInit(): void {
    this.applySearch();
  }

  public applySearch(): void {
    this.loading.set(true);
    const { search, status } = this.searchForm.value;
    this.adminService
      .getCustomers({ search: search || undefined, status: status || undefined })
      .subscribe({
        next: (res) => {
          this.customers.set(res.items || []);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  public openSuspendModal(c: CustomerSummaryDto): void {
    this.targetCustomer.set(c);
    this.suspendReason = '';
    this.showSuspendModal.set(true);
  }

  public confirmSuspend(): void {
    const c = this.targetCustomer();
    if (!c || !this.suspendReason.trim() || this.suspending()) return;

    this.suspending.set(true);
    this.adminService.suspendCustomer(c.id, this.suspendReason).subscribe({
      next: () => {
        this.suspending.set(false);
        this.showSuspendModal.set(false);
        this.notification.success('Customer Suspended', `Customer ${c.fullName} has been suspended.`);
        this.applySearch();
      },
      error: () => this.suspending.set(false),
    });
  }

  public reactivateCustomer(c: CustomerSummaryDto): void {
    this.adminService.reactivateCustomer(c.id).subscribe({
      next: () => {
        this.notification.success('Customer Reactivated', `Customer ${c.fullName} is now active.`);
        this.applySearch();
      },
    });
  }
}
