import { Injectable, signal, effect } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly THEME_KEY = 'apex_core_theme';
  public currentTheme = signal<ThemeMode>('light');

  constructor() {
    const savedTheme = localStorage.getItem(this.THEME_KEY) as ThemeMode | null;
    if (savedTheme === 'dark' || savedTheme === 'light') {
      this.currentTheme.set(savedTheme);
    } else if (typeof window !== 'undefined' && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      this.currentTheme.set('dark');
    }

    effect(() => {
      const mode = this.currentTheme();
      if (typeof document !== 'undefined') {
        const root = document.documentElement;
        if (mode === 'dark') {
          root.classList.add('dark');
        } else {
          root.classList.remove('dark');
        }
        localStorage.setItem(this.THEME_KEY, mode);
      }
    });
  }

  public toggleTheme(): void {
    this.currentTheme.update((current) => (current === 'light' ? 'dark' : 'light'));
  }

  public isDark(): boolean {
    return this.currentTheme() === 'dark';
  }
}
