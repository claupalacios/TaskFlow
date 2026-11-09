import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, Injector, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog, MatDialogConfig, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { ComponentType } from '@angular/cdk/portal';
import { Observable } from 'rxjs';

import { parseApiError } from '../../core/api/api-error';
import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog/confirm-dialog';
import { parseStatusFilter } from './data/status-filter';
import { TASK_STATUSES, TASK_STATUS_LABELS, TaskResponse, TaskStatus } from './data/task.models';
import { TasksStore } from './data/tasks-store';
import {
  TaskFormDialog,
  TaskFormDialogData,
  TaskFormResult,
} from './task-form-dialog/task-form-dialog';
import { TaskItem } from './task-item/task-item';

const GONE_MESSAGE = 'This task no longer exists.';
const FORM_MESSAGES: Record<TaskFormResult, string> = {
  created: 'Task created',
  updated: 'Task updated',
  gone: GONE_MESSAGE,
};

/**
 * The signed-in user's tasks. Owns the `TasksStore` (component provider), shares it with the
 * dialogs it opens, and closes those dialogs when it is destroyed.
 */
@Component({
  selector: 'app-tasks-page',
  imports: [TaskItem, MatButtonModule, MatButtonToggleModule, MatIconModule, MatProgressBarModule],
  // One store per page instance: leaving /tasks (e.g. on logout) discards every task.
  providers: [TasksStore],
  templateUrl: './tasks-page.html',
  styleUrl: './tasks-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TasksPage {
  protected readonly store = inject(TasksStore);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  // This page's injector: dialogs resolve TasksStore from it, so they share this instance.
  private readonly injector = inject(Injector);
  private readonly openDialogs = new Set<MatDialogRef<unknown>>();

  protected readonly statuses = TASK_STATUSES;
  protected readonly labels = TASK_STATUS_LABELS;

  constructor() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed())
      .subscribe((params) => this.store.setFilter(parseStatusFilter(params.get('status'))));
    this.store.load();

    // MatDialog's closeOnNavigation reacts only to history changes (back/forward), not to
    // router navigations such as the one logout makes, so close our dialogs ourselves.
    inject(DestroyRef).onDestroy(() => {
      for (const ref of this.openDialogs) {
        ref.close();
      }
    });
  }

  protected setFilter(value: TaskStatus | 'all'): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { status: value === 'all' ? null : value },
      queryParamsHandling: 'merge',
    });
  }

  protected openForm(task: TaskResponse | null): void {
    this.openDialog<TaskFormDialog, TaskFormDialogData, TaskFormResult>(TaskFormDialog, {
      data: { task },
      width: '560px',
      maxWidth: '95vw',
    }).subscribe((result) => {
      if (result) {
        this.notify(FORM_MESSAGES[result]);
      }
    });
  }

  protected confirmDelete(task: TaskResponse): void {
    this.openDialog<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data: {
        title: 'Delete task?',
        message: `"${task.title}" will be deleted. This cannot be undone.`,
        confirmLabel: 'Delete',
      },
    }).subscribe((confirmed) => {
      if (confirmed) {
        this.store.remove(task.id).subscribe({
          next: () => this.notify('Task deleted'),
          error: (e: unknown) => this.reportError(e),
        });
      }
    });
  }

  protected changeStatus(task: TaskResponse, status: TaskStatus): void {
    this.store.changeStatus(task, status).subscribe({
      next: () => this.notify('Status updated'),
      error: (e: unknown) => this.reportError(e),
    });
  }

  /** Opens a dialog that shares this page's store and is closed with the page. */
  private openDialog<C, D, R>(
    component: ComponentType<C>,
    config: MatDialogConfig<D>,
  ): Observable<R | undefined> {
    const ref = this.dialog.open<C, D, R>(component, { ...config, injector: this.injector });
    this.openDialogs.add(ref);
    const closed = ref.afterClosed();
    closed.subscribe(() => this.openDialogs.delete(ref));
    return closed;
  }

  private reportError(error: unknown): void {
    const gone = error instanceof HttpErrorResponse && error.status === 404;
    this.notify(gone ? GONE_MESSAGE : parseApiError(error).message);
  }

  private notify(message: string): void {
    this.snackBar.open(message, undefined, { duration: 4000 });
  }
}
