import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <header
      class="h-16 border-b border-slate-200/80 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 backdrop-blur-md sticky top-0 z-40 px-6 flex items-center justify-between transition-colors"
    >
      <!-- Left: Logo & Portal Switcher -->
      <div class="flex items-center gap-6">
        <a routerLink="/" class="flex items-center gap-2.5 group">
          <div
            class="w-9 h-9 rounded-xl bg-gradient-to-tr from-sky-600 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-sky-500/20 group-hover:scale-105 transition-transform"
          >
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
              />
            </svg>
          </div>
          <div>
            <div class="font-bold text-base tracking-tight text-slate-900 dark:text-white leading-tight">
              ApexCore
            </div>
            <div class="text-[10px] uppercase font-semibold tracking-wider text-sky-600 dark:text-sky-400">
              Core Banking
            </div>
          </div>
        </a>

        <!-- Role Portal Switcher -->
        @if (authService.isAuthenticated()) {
          <div class="hidden md:flex items-center bg-slate-100 dark:bg-slate-800 p-1 rounded-xl text-xs font-medium">
            <a
              routerLink="/customer"
              routerLinkActive="bg-white dark:bg-slate-700 text-sky-600 dark:text-sky-400 shadow-sm"
              class="px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white transition-all"
            >
              Customer Portal
            </a>
            @if (authService.isAdmin() || authService.isAuditor()) {
              <a
                routerLink="/admin"
                routerLinkActive="bg-white dark:bg-slate-700 text-sky-600 dark:text-sky-400 shadow-sm"
                class="px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white transition-all flex items-center gap-1.5"
              >
                <span>Back-Office Admin</span>
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
              </a>
            }
          </div>
        }
      </div>

      <!-- Right: User info, Theme toggle, Logout -->
      <div class="flex items-center gap-4">
        <!-- Theme Toggle -->
        <button
          (click)="themeService.toggleTheme()"
          class="p-2 rounded-xl text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          title="Toggle Dark/Light Mode"
          aria-label="Toggle theme"
        >
          @if (themeService.isDark()) {
            <!-- Sun icon -->
            <svg class="w-5 h-5 text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 9h-1m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
          } @else {
            <!-- Moon icon -->
            <svg class="w-5 h-5 text-slate-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
            </svg>
          }
        </button>

        @if (authService.isAuthenticated(); as isAuth) {
          <!-- User Profile Brief -->
          <div class="hidden sm:flex items-center gap-3 pl-2 border-l border-slate-200 dark:border-slate-800">
            <div class="text-right">
              <div class="text-xs font-semibold text-slate-900 dark:text-white leading-tight">
                {{ authService.currentUser()?.fullName || 'Banking User' }}
              </div>
              <div class="text-[11px] text-slate-500 dark:text-slate-400 truncate max-w-[140px]">
                {{ authService.currentUser()?.email }}
              </div>
            </div>

            <!-- Avatar -->
            <div
              class="w-8 h-8 rounded-full bg-sky-100 dark:bg-sky-950 text-sky-700 dark:text-sky-300 flex items-center justify-center font-bold text-xs border border-sky-300 dark:border-sky-800"
            >
              {{ (authService.currentUser()?.fullName?.charAt(0) || 'U') | uppercase }}
            </div>
          </div>

          <!-- Logout Button -->
          <button
            (click)="authService.logout()"
            class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 border border-transparent hover:border-rose-200 dark:hover:border-rose-900/50 transition-all"
            title="Log out of session"
          >
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
            <span class="hidden sm:inline">Logout</span>
          </button>
        } @else {
          <div class="flex items-center gap-2">
            <a
              routerLink="/auth/login"
              class="px-3 py-1.5 text-xs font-semibold text-slate-700 dark:text-slate-200 hover:text-sky-600 dark:hover:text-sky-400 transition-colors"
            >
              Sign In
            </a>
            <a
              routerLink="/auth/register"
              class="px-3.5 py-1.5 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 rounded-xl shadow-sm transition-all"
            >
              Register
            </a>
          </div>
        }
      </div>
    </header>
  `,
})
export class HeaderComponent {
  public authService = inject(AuthService);
  public themeService = inject(ThemeService);
}
