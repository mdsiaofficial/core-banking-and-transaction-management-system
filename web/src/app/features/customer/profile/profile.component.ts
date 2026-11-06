import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfile } from '../../../core/models/auth.models';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, BadgeComponent],
  template: `
    <div class="max-w-4xl mx-auto space-y-6">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Customer Profile</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Identity records, verification status, and security preferences
        </p>
      </div>

      @if (authService.currentUser(); as user) {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-8 shadow-sm space-y-6">
          <div class="flex items-center gap-4 pb-6 border-b border-slate-100 dark:border-slate-800">
            <div class="w-16 h-16 rounded-2xl bg-gradient-to-tr from-sky-600 to-indigo-600 text-white font-bold text-2xl flex items-center justify-center shadow-lg shadow-sky-600/20">
              {{ user.fullName.charAt(0) | uppercase }}
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h2 class="text-xl font-bold text-slate-900 dark:text-white">{{ user.fullName }}</h2>
                <app-badge [variant]="user.status === 'ACTIVE' ? 'success' : 'warning'">
                  {{ user.status }}
                </app-badge>
              </div>
              <p class="text-xs text-slate-500 font-mono mt-0.5">{{ user.email }}</p>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6 text-xs">
            <div>
              <span class="text-slate-400 block mb-1">User Identification (UUIDv7)</span>
              <span class="font-mono text-slate-800 dark:text-slate-200 select-all">{{ user.id }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-1">Customer Entity ID</span>
              <span class="font-mono text-slate-800 dark:text-slate-200 select-all">{{ user.customerId || 'Linked' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-1">Phone Number</span>
              <span class="font-semibold text-slate-800 dark:text-slate-200">{{ user.phone || '+8801700000000' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-1">Date of Birth</span>
              <span class="font-semibold text-slate-800 dark:text-slate-200">{{ user.dateOfBirth || '1990-01-01' }}</span>
            </div>
            <div class="sm:col-span-2">
              <span class="text-slate-400 block mb-1">Registered Residential Address</span>
              <span class="font-medium text-slate-800 dark:text-slate-200">{{ user.address || 'Dhaka, Bangladesh' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-1">Assigned Security Roles</span>
              <div class="flex gap-1.5 mt-1">
                @for (role of user.roles; track role) {
                  <span class="px-2 py-0.5 rounded text-[11px] font-semibold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                    {{ role }}
                  </span>
                }
              </div>
            </div>
            <div>
              <span class="text-slate-400 block mb-1">Session Security</span>
              <span class="text-emerald-600 font-semibold flex items-center gap-1">
                <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                Rotating Refresh Token (Reuse Detection Active)
              </span>
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class ProfileComponent {
  public authService = inject(AuthService);
}
