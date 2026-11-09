import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { notImplemented } from '../../../core/not-implemented';
import { CreateTaskRequest, TaskResponse, UpdateTaskRequest } from './task.models';

/** HTTP calls to `/api/tasks`. Holds no state, so it can live in root. */
@Injectable({ providedIn: 'root' })
export class TasksApi {
  list(): Observable<TaskResponse[]> {
    return notImplemented();
  }

  create(request: CreateTaskRequest): Observable<TaskResponse> {
    return notImplemented(request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<TaskResponse> {
    return notImplemented(id, request);
  }

  remove(id: string): Observable<void> {
    return notImplemented(id);
  }
}
