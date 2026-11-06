import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm hover:shadow-md transition-all duration-200"
    >
      <div class="flex items-center justify-between">
        <span class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {{ label() }}
        </span>
        @if (icon()) {
          <div
            class="p-2.5 rounded-xl bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 border border-sky-100 dark:border-sky-900/40"
          >
            <ng-content select="[icon]"></ng-content>
          </div>
        }
      </div>

      <div class="mt-4 flex items-baseline gap-2">
        <span class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
          {{ value() }}
        </span>
        @if (subtext()) {
          <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">
            {{ subtext() }}
          </span>
        }
      </div>

      @if (description()) {
        <p class="mt-2 text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
          {{ description() }}
        </p>
      }
    </div>
  `,
})
export class StatCardComponent {
  label = input.required<string>();
  value = input.required<string>();
  subtext = input<string>();
  description = input<string>();
  icon = input<boolean>(true);
}
