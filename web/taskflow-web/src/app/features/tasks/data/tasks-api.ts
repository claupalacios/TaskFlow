import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CreateTaskRequest, TaskResponse, UpdateTaskRequest } from './task.models';

const BASE_URL = '/api/tasks';

/** HTTP calls to `/api/tasks`. Holds no state, so it can live in root. */
@Injectable({ providedIn: 'root' })
export class TasksApi {
  private readonly http = inject(HttpClient);

  list(): Observable<TaskResponse[]> {
    return this.http.get<TaskResponse[]>(BASE_URL);
  }

  create(request: CreateTaskRequest): Observable<TaskResponse> {
    return this.http.post<TaskResponse>(BASE_URL, request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<TaskResponse> {
    return this.http.put<TaskResponse>(taskUrl(id), request);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(taskUrl(id));
  }
}

function taskUrl(id: string): string {
  return `${BASE_URL}/${encodeURIComponent(id)}`;
}
