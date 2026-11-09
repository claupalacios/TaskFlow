import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MonoTypeOperatorFunction, Observable, catchError, tap, throwError } from 'rxjs';

import { parseApiError } from '../../../core/api/api-error';
import {
  CreateTaskRequest,
  TASK_STATUSES,
  TaskCounts,
  TaskResponse,
  TaskStatus,
  UpdateTaskRequest,
} from './task.models';
import { TasksApi } from './tasks-api';

/**
 * The signed-in user's tasks. Provided by `TasksPage` (never in root or on the route),
 * so the state is discarded with the page and in-flight requests are cancelled.
 */
@Injectable()
export class TasksStore {
  private readonly api = inject(TasksApi);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _tasks = signal<TaskResponse[]>([]);
  private readonly _loading = signal(true);
  private readonly _error = signal<string | null>(null);
  private readonly _filter = signal<TaskStatus | null>(null);

  /** Every task, newest first. Empty until the first load completes. */
  readonly tasks = this._tasks.asReadonly();
  /** True from creation until a load finishes. */
  readonly loading = this._loading.asReadonly();
  /** Message of the last failed load; null after a successful one. */
  readonly error = this._error.asReadonly();
  /** Status shown; null means all. */
  readonly filter = this._filter.asReadonly();

  readonly visibleTasks = computed(() => {
    const filter = this._filter();
    const tasks = this._tasks();
    return filter ? tasks.filter((t) => t.status === filter) : tasks;
  });

  readonly counts = computed<TaskCounts>(() => {
    const tasks = this._tasks();
    const counts = { all: tasks.length } as TaskCounts;
    for (const status of TASK_STATUSES) {
      counts[status] = tasks.filter((t) => t.status === status).length;
    }
    return counts;
  });

  setFilter(status: TaskStatus | null): void {
    this._filter.set(status);
  }

  /** Fetches the list. Errors are kept in `error`, not thrown. */
  load(): void {
    this._loading.set(true);
    this._error.set(null);
    this.api
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tasks) => {
          this._tasks.set(tasks);
          this._loading.set(false);
        },
        error: (e: unknown) => {
          this._error.set(parseApiError(e).message);
          this._loading.set(false);
        },
      });
  }

  /** Creates the task and adds it at the top. Errors are passed through. */
  create(request: CreateTaskRequest): Observable<TaskResponse> {
    return this.api.create(request).pipe(
      tap((created) => this._tasks.update((tasks) => [created, ...tasks])),
      takeUntilDestroyed(this.destroyRef),
    );
  }

  /** Replaces the task. A 404 removes it from the list; errors are passed through. */
  update(id: string, request: UpdateTaskRequest): Observable<TaskResponse> {
    return this.api.update(id, request).pipe(
      tap((updated) =>
        this._tasks.update((tasks) => tasks.map((t) => (t.id === id ? updated : t))),
      ),
      this.dropWhenGone(id),
      takeUntilDestroyed(this.destroyRef),
    );
  }

  /** Sends the full task with only the status changed. Same error rules as `update`. */
  changeStatus(task: TaskResponse, status: TaskStatus): Observable<TaskResponse> {
    const { title, description, dueDate } = task;
    return this.update(task.id, { title, description, status, dueDate });
  }

  /** Deletes the task. A 404 also removes it from the list; errors are passed through. */
  remove(id: string): Observable<void> {
    return this.api.remove(id).pipe(
      tap(() => this.drop(id)),
      this.dropWhenGone(id),
      takeUntilDestroyed(this.destroyRef),
    );
  }

  private drop(id: string): void {
    this._tasks.update((tasks) => tasks.filter((t) => t.id !== id));
  }

  /** On a 404 the task no longer exists for this user: drop it, then let the caller report it. */
  private dropWhenGone<T>(id: string): MonoTypeOperatorFunction<T> {
    return catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.drop(id);
      }
      return throwError(() => error);
    });
  }
}
