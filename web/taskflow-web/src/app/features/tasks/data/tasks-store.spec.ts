import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EnvironmentInjector, createEnvironmentInjector } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { TaskResponse } from './task.models';
import { TasksStore } from './tasks-store';

function task(id: string, overrides: Partial<TaskResponse> = {}): TaskResponse {
  return {
    id,
    title: `Task ${id}`,
    description: `About ${id}`,
    status: 'Pending',
    dueDate: '2026-11-20',
    createdAt: '2026-11-09T12:00:00+00:00',
    updatedAt: '2026-11-09T12:00:00+00:00',
    ...overrides,
  };
}

const PENDING = task('t-1');
const IN_PROGRESS = task('t-2', { status: 'InProgress', dueDate: null });
const COMPLETED = task('t-3', { status: 'Completed', description: '' });

const NOT_FOUND = { status: 404, statusText: 'Not Found' };
const notFoundBody = { status: 404, title: 'Not found', detail: 'Task was not found.' };

describe('TasksStore', () => {
  let http: HttpTestingController;
  let injector: EnvironmentInjector;
  let store: TasksStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    // Its own injector, so a test can destroy the store the way TasksPage does.
    injector = createEnvironmentInjector([TasksStore], TestBed.inject(EnvironmentInjector));
    store = injector.get(TasksStore);
  });

  afterEach(() => http.verify());

  function loadWith(tasks: TaskResponse[]): void {
    store.load();
    http.expectOne('/api/tasks').flush(tasks);
  }

  const ids = (tasks: TaskResponse[]) => tasks.map((t) => t.id);

  it('starts empty and loading, with no error', () => {
    expect(store.tasks()).toEqual([]);
    expect(store.loading()).toBe(true);
    expect(store.error()).toBeNull();
    expect(store.filter()).toBeNull();
  });

  it('loads the tasks and stops loading', () => {
    loadWith([PENDING, IN_PROGRESS]);

    expect(store.tasks()).toEqual([PENDING, IN_PROGRESS]);
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('keeps the API message when loading fails', () => {
    store.load();
    http
      .expectOne('/api/tasks')
      .flush(
        { status: 500, title: 'Error', detail: 'Database unavailable.' },
        { status: 500, statusText: 'Server Error' },
      );

    expect(store.error()).toBe('Database unavailable.');
    expect(store.loading()).toBe(false);
    expect(store.tasks()).toEqual([]);
  });

  it('clears the error and shows loading again on retry', () => {
    store.load();
    http.expectOne('/api/tasks').flush(null, { status: 500, statusText: 'Server Error' });

    store.load();

    expect(store.error()).toBeNull();
    expect(store.loading()).toBe(true);
    http.expectOne('/api/tasks').flush([PENDING]);
    expect(store.tasks()).toEqual([PENDING]);
  });

  it('shows every task without a filter and only the matching ones with it', () => {
    loadWith([PENDING, IN_PROGRESS, COMPLETED]);

    expect(ids(store.visibleTasks())).toEqual(['t-1', 't-2', 't-3']);

    store.setFilter('InProgress');
    expect(store.filter()).toBe('InProgress');
    expect(ids(store.visibleTasks())).toEqual(['t-2']);

    store.setFilter(null);
    expect(ids(store.visibleTasks())).toEqual(['t-1', 't-2', 't-3']);
  });

  it('counts the tasks per status', () => {
    loadWith([PENDING, task('t-4'), COMPLETED]);

    expect(store.counts()).toEqual({ all: 3, Pending: 2, InProgress: 0, Completed: 1 });
  });

  it('creates a task and adds it at the top', () => {
    loadWith([PENDING]);
    const created = task('t-9', { title: 'New one' });
    const request = { title: 'New one', description: '', dueDate: null };
    let emitted: TaskResponse | undefined;

    store.create(request).subscribe((t) => (emitted = t));
    const req = http.expectOne('/api/tasks');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(created, { status: 201, statusText: 'Created' });

    expect(emitted).toEqual(created);
    expect(ids(store.tasks())).toEqual(['t-9', 't-1']);
  });

  it('passes a failed create through and leaves the list alone', () => {
    loadWith([PENDING]);
    let status: number | undefined;

    store
      .create({ title: '', description: '', dueDate: null })
      .subscribe({ error: (e) => (status = e.status) });
    http
      .expectOne('/api/tasks')
      .flush({ title: 'Title is required.' }, { status: 400, statusText: 'Bad Request' });

    expect(status).toBe(400);
    expect(store.tasks()).toEqual([PENDING]);
  });

  it('updates a task in place', () => {
    loadWith([PENDING, IN_PROGRESS]);
    const request = {
      title: 'Renamed',
      description: 'x',
      status: 'Completed' as const,
      dueDate: null,
    };
    const updated = task('t-1', { ...request });

    store.update('t-1', request).subscribe();
    const req = http.expectOne('/api/tasks/t-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(updated);

    expect(store.tasks()).toEqual([updated, IN_PROGRESS]);
  });

  it('removes a task that no longer exists when updating it, and passes the 404 on', () => {
    loadWith([PENDING, IN_PROGRESS]);
    let status: number | undefined;

    store
      .update('t-1', { title: 'x', description: '', status: 'Pending', dueDate: null })
      .subscribe({ error: (e) => (status = e.status) });
    http.expectOne('/api/tasks/t-1').flush(notFoundBody, NOT_FOUND);

    expect(status).toBe(404);
    expect(ids(store.tasks())).toEqual(['t-2']);
  });

  it('keeps the task when an update fails for another reason', () => {
    loadWith([PENDING]);

    store
      .update('t-1', { title: '', description: '', status: 'Pending', dueDate: null })
      .subscribe({ error: () => undefined });
    http.expectOne('/api/tasks/t-1').flush(null, { status: 400, statusText: 'Bad Request' });

    expect(store.tasks()).toEqual([PENDING]);
  });

  it('changes only the status, sending title, description and due date unchanged', () => {
    loadWith([PENDING]);

    store.changeStatus(PENDING, 'InProgress').subscribe();
    const req = http.expectOne('/api/tasks/t-1');

    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      title: PENDING.title,
      description: PENDING.description,
      status: 'InProgress',
      dueDate: PENDING.dueDate,
    });
    req.flush({ ...PENDING, status: 'InProgress' });
    expect(store.tasks()[0].status).toBe('InProgress');
  });

  it('sends an empty description and a missing due date as they are when changing the status', () => {
    const bare = task('t-5', { description: '', dueDate: null });
    loadWith([bare]);

    store.changeStatus(bare, 'Completed').subscribe();
    const req = http.expectOne('/api/tasks/t-5');

    expect(req.request.body).toEqual({
      title: bare.title,
      description: '',
      status: 'Completed',
      dueDate: null,
    });
    req.flush({ ...bare, status: 'Completed' });
  });

  it('sends no fields beyond the update contract when changing the status', () => {
    loadWith([PENDING]);

    store.changeStatus(PENDING, 'Completed').subscribe();
    const req = http.expectOne('/api/tasks/t-1');

    expect(Object.keys(req.request.body).sort()).toEqual([
      'description',
      'dueDate',
      'status',
      'title',
    ]);
    req.flush({ ...PENDING, status: 'Completed' });
  });

  it('deletes a task and removes it from the list', () => {
    loadWith([PENDING, IN_PROGRESS]);
    let completed = false;

    store.remove('t-2').subscribe({ complete: () => (completed = true) });
    const req = http.expectOne('/api/tasks/t-2');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
    expect(ids(store.tasks())).toEqual(['t-1']);
  });

  it('removes the task when deleting finds it already gone, and passes the 404 on', () => {
    loadWith([PENDING, IN_PROGRESS]);
    let status: number | undefined;

    store.remove('t-2').subscribe({ error: (e) => (status = e.status) });
    http.expectOne('/api/tasks/t-2').flush(notFoundBody, NOT_FOUND);

    expect(status).toBe(404);
    expect(ids(store.tasks())).toEqual(['t-1']);
  });

  it('keeps the task when a delete fails for another reason', () => {
    loadWith([PENDING]);

    store.remove('t-1').subscribe({ error: () => undefined });
    http.expectOne('/api/tasks/t-1').flush(null, { status: 500, statusText: 'Server Error' });

    expect(store.tasks()).toEqual([PENDING]);
  });

  it('cancels an in-flight load when destroyed, so a late response is never stored', () => {
    store.load();
    const req = http.expectOne('/api/tasks');

    injector.destroy();

    expect(req.cancelled).toBe(true);
    expect(store.tasks()).toEqual([]);
  });

  it('cancels an in-flight write when destroyed', () => {
    loadWith([PENDING]);
    store.changeStatus(PENDING, 'Completed').subscribe();
    const req = http.expectOne('/api/tasks/t-1');

    injector.destroy();

    expect(req.cancelled).toBe(true);
  });
});
