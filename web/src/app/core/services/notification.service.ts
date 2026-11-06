import { Injectable, signal } from '@angular/core';
import { ProblemDetails } from '../models/problem-details.model';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface ToastNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  errorCode?: string;
  traceId?: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private notificationsSignal = signal<ToastNotification[]>([]);
  public readonly notifications = this.notificationsSignal.asReadonly();

  public show(notification: Omit<ToastNotification, 'id'>): string {
    const id = Math.random().toString(36).substring(2, 9);
    const item: ToastNotification = {
      ...notification,
      id,
      duration: notification.duration ?? 5000,
    };

    this.notificationsSignal.update((list) => [...list, item]);

    if (item.duration && item.duration > 0) {
      setTimeout(() => {
        this.dismiss(id);
      }, item.duration);
    }

    return id;
  }

  public success(title: string, message: string, duration?: number): string {
    return this.show({ type: 'success', title, message, duration });
  }

  public warning(title: string, message: string, duration?: number): string {
    return this.show({ type: 'warning', title, message, duration });
  }

  public info(title: string, message: string, duration?: number): string {
    return this.show({ type: 'info', title, message, duration });
  }

  public error(title: string, message: string, errorCode?: string, traceId?: string): string {
    return this.show({
      type: 'error',
      title,
      message,
      errorCode,
      traceId,
      duration: 8000, // Keep errors visible longer
    });
  }

  public fromProblemDetails(problem: ProblemDetails): string {
    const title = problem.title || 'Operation Failed';
    const detail = problem.detail || 'An unexpected banking error occurred.';
    const errorCode = problem.errorCode || problem.error_code;
    const traceId = problem.traceId || problem.trace_id;

    // Check if there are field errors
    let fieldErrorsMsg = '';
    if (problem.errors && Object.keys(problem.errors).length > 0) {
      const messages = Object.entries(problem.errors).map(
        ([field, errs]) => `${field}: ${errs.join(', ')}`
      );
      fieldErrorsMsg = `\n${messages.join('\n')}`;
    }

    return this.error(title, `${detail}${fieldErrorsMsg}`, errorCode, traceId);
  }

  public dismiss(id: string): void {
    this.notificationsSignal.update((list) => list.filter((n) => n.id !== id));
  }

  public clearAll(): void {
    this.notificationsSignal.set([]);
  }
}
