import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { TaskResponse } from './task.models';
import { TasksApi } from './tasks-api';

const TASK: TaskResponse = {
  id: 't-1',
  title: 'Write report',
  description: 'Quarterly numbers',
  status: 'Pending',
  dueDate: '2026-11-20',
  createdAt: '2026-11-09T12:00:00+00:00',
  updatedAt: '2026-11-09T12:00:00+00:00',
};

describe('TasksApi', () => {
  let api: TasksApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(TasksApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists the tasks with a GET to /api/tasks', () => {
    let result: TaskResponse[] | undefined;

    api.list().subscribe((tasks) => (result = tasks));
    const req = http.expectOne('/api/tasks');
    expect(req.request.method).toBe('GET');
    req.flush([TASK]);

    expect(result).toEqual([TASK]);
  });

  it('creates with a POST of the request body', () => {
    const body = { title: 'Write report', description: '', dueDate: null };
    let result: TaskResponse | undefined;

    api.create(body).subscribe((task) => (result = task));
    const req = http.expectOne('/api/tasks');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(TASK, { status: 201, statusText: 'Created' });

    expect(result).toEqual(TASK);
  });

  it('updates with a PUT of the full task to /api/tasks/{id}', () => {
    const body = {
      title: 'Write report',
      description: 'Quarterly numbers',
      status: 'Completed' as const,
      dueDate: '2026-11-20',
    };

    api.update('t-1', body).subscribe();
    const req = http.expectOne('/api/tasks/t-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ ...TASK, status: 'Completed' });
  });

  it('deletes with a DELETE to /api/tasks/{id}', () => {
    let completed = false;

    api.remove('t-1').subscribe({ complete: () => (completed = true) });
    const req = http.expectOne('/api/tasks/t-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('escapes the id in the URL', () => {
    api.remove('a/b').subscribe();

    http.expectOne('/api/tasks/a%2Fb').flush(null, { status: 204, statusText: 'No Content' });
  });
});
