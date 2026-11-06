import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AdminService, AuditLogSearchParams } from '../../../core/services/admin.service';
import { AuditLogDto } from '../../../core/models/admin.models';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BadgeComponent, ModalComponent],
  template: `
    <div class="space-y-6 max-w-7xl mx-auto">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Security & Regulatory Audit Trail</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Append-only, tamper-evident audit logs capturing administrative and monetary actions with correlation IDs
        </p>
      </div>

      <!-- Filter Bar -->
      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm">
        <form [formGroup]="filterForm" (ngSubmit)="applyFilters()" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">Actor User ID / Email</label>
            <input
              type="text"
              formControlName="actor"
              placeholder="e.g. admin@apexbank.local"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            />
          </div>

          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">Action</label>
            <select
              formControlName="action"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            >
              <option value="">All Actions</option>
              <option value="CUSTOMER_REGISTERED">CUSTOMER_REGISTERED</option>
              <option value="LOGIN_SUCCESS">LOGIN_SUCCESS</option>
              <option value="ACCOUNT_OPENED">ACCOUNT_OPENED</option>
              <option value="ACCOUNT_FROZEN">ACCOUNT_FROZEN</option>
              <option value="ACCOUNT_UNFROZEN">ACCOUNT_UNFROZEN</option>
              <option value="TRANSFER_POSTED">TRANSFER_POSTED</option>
              <option value="TRANSACTION_REVERSED">TRANSACTION_REVERSED</option>
              <option value="RECONCILIATION_RUN">RECONCILIATION_RUN</option>
            </select>
          </div>

          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">Entity Type</label>
            <select
              formControlName="entityType"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            >
              <option value="">All Entities</option>
              <option value="Customer">Customer</option>
              <option value="Account">Account</option>
              <option value="LedgerTransaction">LedgerTransaction</option>
              <option value="Reconciliation">Reconciliation</option>
            </select>
          </div>

          <div>
            <label class="block text-[11px] font-semibold uppercase text-slate-500 mb-1">From Date</label>
            <input
              type="date"
              formControlName="from"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
            />
          </div>

          <div class="flex items-end gap-2">
            <button
              type="submit"
              class="flex-1 py-2 px-3 rounded-xl text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 shadow-sm"
            >
              Filter Logs
            </button>
            <button
              type="button"
              (click)="resetFilters()"
              class="py-2 px-3 rounded-xl text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200"
            >
              Reset
            </button>
          </div>
        </form>
      </div>

      <!-- Audit Logs Table -->
      @if (loading()) {
        <div class="space-y-3">
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
        </div>
      } @else if (logs().length === 0) {
        <div class="p-12 rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 text-center bg-white dark:bg-slate-900">
          <h3 class="font-semibold text-slate-900 dark:text-white">No audit records found</h3>
          <p class="text-xs text-slate-500 mt-1">Audit log is append-only and query filtered.</p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm text-slate-600 dark:text-slate-300">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-[11px] uppercase font-bold text-slate-400 border-b border-slate-200/70 dark:border-slate-800">
                <tr>
                  <th class="px-6 py-4">Timestamp</th>
                  <th class="px-6 py-4">Action</th>
                  <th class="px-6 py-4">Actor & Role</th>
                  <th class="px-6 py-4">Target Entity</th>
                  <th class="px-6 py-4">Outcome</th>
                  <th class="px-6 py-4 text-right">Details</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800 font-mono text-xs">
                @for (log of logs(); track log.id) {
                  <tr class="hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors">
                    <td class="px-6 py-4 text-slate-500 font-sans">
                      {{ log.occurredAt | date: 'medium' }}
                    </td>
                    <td class="px-6 py-4 font-bold text-slate-900 dark:text-white">
                      {{ log.action }}
                    </td>
                    <td class="px-6 py-4 font-sans">
                      <div>{{ log.actorEmail || log.actorUserId }}</div>
                      <div class="text-[10px] text-slate-400">{{ log.actorRole }} · {{ log.actorIp }}</div>
                    </td>
                    <td class="px-6 py-4 font-sans">
                      <span class="font-semibold text-slate-700 dark:text-slate-300">{{ log.entityType }}</span>
                      <div class="text-[10px] text-slate-400 font-mono truncate max-w-[120px]">{{ log.entityId }}</div>
                    </td>
                    <td class="px-6 py-4 font-sans">
                      <app-badge [variant]="log.outcome === 'SUCCESS' ? 'success' : log.outcome === 'FAILURE' ? 'danger' : 'warning'">
                        {{ log.outcome }}
                      </app-badge>
                    </td>
                    <td class="px-6 py-4 text-right font-sans">
                      <button
                        type="button"
                        (click)="inspectLog(log)"
                        class="px-2.5 py-1 text-xs font-semibold text-sky-600 bg-sky-50 dark:bg-sky-950/60 hover:bg-sky-100 rounded-lg transition-colors"
                      >
                        Inspect Diff
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          @if (nextCursor()) {
            <div class="p-4 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-100 dark:border-slate-800 text-center">
              <button
                type="button"
                (click)="loadMore()"
                [disabled]="loadingMore()"
                class="px-4 py-2 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 hover:bg-slate-50 transition-all shadow-sm"
              >
                {{ loadingMore() ? 'Loading More...' : 'Load More Audit Records' }}
              </button>
            </div>
          }
        </div>
      }

      <!-- Audit Detail Drawer Modal -->
      <app-modal
        [isOpen]="showModal()"
        title="Audit Log State Diff Inspection"
        [showFooter]="true"
        (close)="showModal.set(false)"
      >
        @if (selectedLog(); as item) {
          <div class="space-y-4 text-xs font-sans">
            <div class="grid grid-cols-2 gap-3 p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700">
              <div>
                <span class="text-slate-400 block text-[10px] uppercase font-bold">Action</span>
                <span class="font-bold text-slate-900 dark:text-white">{{ item.action }}</span>
              </div>
              <div>
                <span class="text-slate-400 block text-[10px] uppercase font-bold">Correlation ID</span>
                <span class="font-mono text-slate-700 dark:text-slate-300 select-all">{{ item.correlationId }}</span>
              </div>
              <div>
                <span class="text-slate-400 block text-[10px] uppercase font-bold">Actor IP</span>
                <span class="font-mono">{{ item.actorIp }}</span>
              </div>
              <div>
                <span class="text-slate-400 block text-[10px] uppercase font-bold">Timestamp (UTC)</span>
                <span>{{ item.occurredAt }}</span>
              </div>
            </div>

            <!-- Before & After State Comparison -->
            <div class="space-y-3 font-mono text-xs">
              <div>
                <span class="text-[11px] font-sans font-bold text-slate-500 uppercase block mb-1">
                  Before State (Snapshot)
                </span>
                <pre class="p-3 bg-slate-100 dark:bg-slate-950 rounded-xl overflow-x-auto text-[11px] max-h-40 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200">{{ item.before ? (item.before | json) : 'None (Creation Event)' }}</pre>
              </div>

              <div>
                <span class="text-[11px] font-sans font-bold text-slate-500 uppercase block mb-1">
                  After State (Snapshot)
                </span>
                <pre class="p-3 bg-slate-100 dark:bg-slate-950 rounded-xl overflow-x-auto text-[11px] max-h-40 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200">{{ item.after ? (item.after | json) : 'None (No state modification)' }}</pre>
              </div>
            </div>
          </div>
        }

        <div modal-actions class="flex items-center gap-2">
          <button
            type="button"
            (click)="showModal.set(false)"
            class="px-4 py-2 rounded-xl text-xs font-semibold text-white bg-slate-900 dark:bg-slate-700 hover:bg-slate-800"
          >
            Close Inspector
          </button>
        </div>
      </app-modal>
    </div>
  `,
})
export class AuditLogViewerComponent implements OnInit {
  private fb = inject(FormBuilder);
  private adminService = inject(AdminService);

  public logs = signal<AuditLogDto[]>([]);
  public loading = signal(true);
  public loadingMore = signal(false);
  public nextCursor = signal<string | null>(null);

  public selectedLog = signal<AuditLogDto | null>(null);
  public showModal = signal(false);

  public filterForm = this.fb.group({
    actor: [''],
    action: [''],
    entityType: [''],
    from: [''],
  });

  ngOnInit(): void {
    this.applyFilters();
  }

  public applyFilters(): void {
    this.loading.set(true);
    const val = this.filterForm.value;
    const params: AuditLogSearchParams = {
      actor: val.actor || undefined,
      action: val.action || undefined,
      entityType: val.entityType || undefined,
      from: val.from || undefined,
      limit: 25,
    };

    this.adminService.getAuditLogs(params).subscribe({
      next: (res) => {
        this.logs.set(res.items || []);
        this.nextCursor.set(res.nextCursor);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  public resetFilters(): void {
    this.filterForm.reset();
    this.applyFilters();
  }

  public loadMore(): void {
    const cursor = this.nextCursor();
    if (!cursor || this.loadingMore()) return;

    this.loadingMore.set(true);
    const val = this.filterForm.value;

    this.adminService
      .getAuditLogs({
        cursor,
        actor: val.actor || undefined,
        action: val.action || undefined,
        entityType: val.entityType || undefined,
        from: val.from || undefined,
        limit: 25,
      })
      .subscribe({
        next: (res) => {
          this.logs.update((curr) => [...curr, ...(res.items || [])]);
          this.nextCursor.set(res.nextCursor);
          this.loadingMore.set(false);
        },
        error: () => this.loadingMore.set(false),
      });
  }

  public inspectLog(log: AuditLogDto): void {
    this.selectedLog.set(log);
    this.showModal.set(true);
  }
}
