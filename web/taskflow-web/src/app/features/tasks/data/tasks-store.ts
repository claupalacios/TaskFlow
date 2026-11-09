import { Injectable, computed, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { notImplemented } from '../../../core/not-implemented';
import {
  CreateTaskRequest,
  TaskCounts,
  TaskResponse,
  TaskStatus,
  UpdateTaskRequest,
} from './task.models';

/**
 * The signed-in user's tasks. Provided by `TasksPage` (never in root or on the route),
 * so the state is discarded with the page and in-flight requests are cancelled.
 */
@Injectable()
export class TasksStore {
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

  readonly visibleTasks = computed<TaskResponse[]>(() => notImplemented(this._tasks()));
  readonly counts = computed<TaskCounts>(() => notImplemented(this._tasks()));

  setFilter(status: TaskStatus | null): void {
    notImplemented(status);
  }

  /** Fetches the list. Errors are kept in `error`, not thrown. */
  load(): void {
    notImplemented();
  }

  /** Creates the task and adds it at the top. Errors are passed through. */
  create(request: CreateTaskRequest): Observable<TaskResponse> {
    return notImplemented(request);
  }

  /** Replaces the task. A 404 removes it from the list; errors are passed through. */
  update(id: string, request: UpdateTaskRequest): Observable<TaskResponse> {
    return notImplemented(id, request);
  }

  /** Sends the full task with only the status changed. Same error rules as `update`. */
  changeStatus(task: TaskResponse, status: TaskStatus): Observable<TaskResponse> {
    return notImplemented(task, status);
  }

  /** Deletes the task. A 404 also removes it from the list; errors are passed through. */
  remove(id: string): Observable<void> {
    return notImplemented(id);
  }
}
