import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';

import {
  buttonWithText,
  control,
  openDialogElements,
  overlayText,
  query,
  submitButton,
  typeInto,
} from '../../../testing/dom';
import { noMaterialAnimations } from '../../../testing/material';
import { makeTask } from '../../../testing/tasks';
import { TaskResponse } from './data/task.models';
import { TasksStore } from './data/tasks-store';
import { TasksPage } from './tasks-page';

@Component({ template: '' })
class Blank {}

const PENDING = makeTask('t-1', { title: 'Write report', status: 'Pending' });
const IN_PROGRESS = makeTask('t-2', { title: 'Book flights', status: 'InProgress' });
const COMPLETED = makeTask('t-3', { title: 'Pay rent', status: 'Completed', dueDate: null });
const ALL = [PENDING, IN_PROGRESS, COMPLETED];

describe('TasksPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        noMaterialAnimations,
        provideRouter([
          { path: 'tasks', component: TasksPage },
          { path: 'login', component: Blank },
        ]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => http.verify());

  // HttpClient keeps the app unstable while a request is open, so never await
  // stability before the pending request has been flushed.
  const settle = () => harness.fixture.whenStable();
  const root = () => harness.routeNativeElement as HTMLElement;
  const currentUrl = () => TestBed.inject(Router).url;
  const pageStore = () => harness.routeDebugElement!.injector.get(TasksStore);

  async function open(url = '/tasks', tasks: TaskResponse[] = ALL): Promise<HTMLElement> {
    await harness.navigateByUrl(url, TasksPage);
    http.expectOne('/api/tasks').flush(tasks);
    await settle();
    return root();
  }

  const titles = (el: HTMLElement) =>
    Array.from(el.querySelectorAll('app-task-item h3')).map((h) => h.textContent?.trim());

  function item(el: HTMLElement, title: string): HTMLElement {
    const found = Array.from(el.querySelectorAll<HTMLElement>('app-task-item')).find(
      (i) => i.querySelector('h3')?.textContent?.trim() === title,
    );
    if (!found) {
      throw new Error(`No task titled "${title}"`);
    }
    return found;
  }

  const action = (el: HTMLElement, title: string, label: string) =>
    query<HTMLButtonElement>(item(el, title), `button[aria-label^="${label}"]`);

  function filterToggle(el: HTMLElement, label: string): HTMLButtonElement {
    const toggle = Array.from(el.querySelectorAll<HTMLElement>('mat-button-toggle')).find((t) =>
      t.textContent?.includes(label),
    );
    if (!toggle) {
      throw new Error(`No filter "${label}"`);
    }
    return query<HTMLButtonElement>(toggle, 'button');
  }

  describe('loading', () => {
    it('shows a loading message, then the tasks newest first', async () => {
      await harness.navigateByUrl('/tasks', TasksPage);

      expect(root().textContent).toContain('Loading tasks');
      expect(titles(root())).toEqual([]);

      http.expectOne('/api/tasks').flush(ALL);
      await settle();

      expect(root().textContent).not.toContain('Loading tasks');
      expect(titles(root())).toEqual(['Write report', 'Book flights', 'Pay rent']);
    });

    it('says so when there are no tasks yet', async () => {
      const el = await open('/tasks', []);

      expect(el.textContent).toContain('No tasks yet');
    });

    it('shows the error and loads again on retry', async () => {
      await harness.navigateByUrl('/tasks', TasksPage);
      http
        .expectOne('/api/tasks')
        .flush(
          { status: 500, title: 'Error', detail: 'Database unavailable.' },
          { status: 500, statusText: 'Server Error' },
        );
      await settle();

      expect(query(root(), '[role="alert"]').textContent).toContain('Database unavailable.');

      buttonWithText(root(), 'Retry').click();
      harness.fixture.detectChanges();
      http.expectOne('/api/tasks').flush(ALL);
      await settle();

      expect(root().querySelector('[role="alert"]')).toBeNull();
      expect(titles(root())).toHaveLength(3);
    });
  });

  describe('filter', () => {
    it('shows only the status in the URL', async () => {
      const el = await open('/tasks?status=Pending');

      expect(titles(el)).toEqual(['Write report']);
    });

    it('shows every task when the status in the URL is not valid', async () => {
      const el = await open('/tasks?status=Done');

      expect(titles(el)).toHaveLength(3);
    });

    it('puts the chosen status in the URL, and removes it for All', async () => {
      const el = await open();

      filterToggle(el, 'In progress').click();
      await settle();
      expect(currentUrl()).toBe('/tasks?status=InProgress');
      expect(titles(el)).toEqual(['Book flights']);

      filterToggle(el, 'All').click();
      await settle();
      expect(currentUrl()).toBe('/tasks');
      expect(titles(el)).toHaveLength(3);
    });

    it('shows how many tasks each filter holds', async () => {
      const el = await open('/tasks', [...ALL, makeTask('t-4', { status: 'Pending' })]);

      expect(filterToggle(el, 'All').textContent).toContain('4');
      expect(filterToggle(el, 'Pending').textContent).toContain('2');
      expect(filterToggle(el, 'Completed').textContent).toContain('1');
    });

    it('says so when no task has the chosen status', async () => {
      const el = await open('/tasks?status=Completed', [PENDING]);

      expect(el.textContent).toContain('No tasks with this status');
    });
  });

  describe('creating and editing', () => {
    it('creates a task from the dialog and shows it at the top', async () => {
      const el = await open();

      buttonWithText(el, 'New task').click();
      await settle();
      const dialog = openDialogElements()[0];
      typeInto(control(dialog, 'title'), 'Call the bank');
      submitButton(dialog).click();
      const req = http.expectOne('/api/tasks');
      expect(req.request.method).toBe('POST');
      req.flush(makeTask('t-9', { title: 'Call the bank' }), {
        status: 201,
        statusText: 'Created',
      });
      await settle();

      expect(openDialogElements()).toHaveLength(0);
      expect(titles(el)[0]).toBe('Call the bank');
      expect(overlayText()).toContain('Task created');
    });

    it('edits a task from the dialog', async () => {
      const el = await open();

      action(el, 'Book flights', 'Edit').click();
      await settle();
      const dialog = openDialogElements()[0];
      expect(control(dialog, 'title').value).toBe('Book flights');
      typeInto(control(dialog, 'title'), 'Book the flights');
      submitButton(dialog).click();
      const req = http.expectOne('/api/tasks/t-2');
      expect(req.request.method).toBe('PUT');
      req.flush({ ...IN_PROGRESS, title: 'Book the flights' });
      await settle();

      expect(titles(el)).toEqual(['Write report', 'Book the flights', 'Pay rent']);
      expect(overlayText()).toContain('Task updated');
    });

    it('reports a task that was deleted elsewhere while editing it', async () => {
      const el = await open();

      action(el, 'Book flights', 'Edit').click();
      await settle();
      submitButton(openDialogElements()[0]).click();
      http
        .expectOne('/api/tasks/t-2')
        .flush({ status: 404, title: 'Not found' }, { status: 404, statusText: 'Not Found' });
      await settle();

      expect(titles(el)).toEqual(['Write report', 'Pay rent']);
      expect(overlayText()).toContain('This task no longer exists.');
    });
  });

  describe('changing the status', () => {
    it('sends the full task with the new status and shows the change', async () => {
      const el = await open();

      action(el, 'Write report', 'Change status').click();
      await settle();
      const completed = Array.from(
        document.querySelectorAll<HTMLButtonElement>('[mat-menu-item]'),
      ).find((o) => o.textContent?.includes('Completed'));
      completed?.click();
      const req = http.expectOne('/api/tasks/t-1');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({
        title: PENDING.title,
        description: PENDING.description,
        status: 'Completed',
        dueDate: PENDING.dueDate,
      });
      req.flush({ ...PENDING, status: 'Completed' });
      await settle();

      expect(item(el, 'Write report').textContent).toContain('Completed');
      expect(overlayText()).toContain('Status updated');
    });

    it('shows the API message and keeps the status when the change fails', async () => {
      const el = await open();

      action(el, 'Write report', 'Change status').click();
      await settle();
      Array.from(document.querySelectorAll<HTMLButtonElement>('[mat-menu-item]'))
        .find((o) => o.textContent?.includes('Completed'))
        ?.click();
      http
        .expectOne('/api/tasks/t-1')
        .flush(
          { status: 500, title: 'Error', detail: 'Database unavailable.' },
          { status: 500, statusText: 'Server Error' },
        );
      await settle();

      expect(item(el, 'Write report').textContent).toContain('Pending');
      expect(overlayText()).toContain('Database unavailable.');
    });
  });

  describe('deleting', () => {
    async function askToDelete(el: HTMLElement, title: string): Promise<HTMLElement> {
      action(el, title, 'Delete').click();
      await settle();
      return openDialogElements()[0];
    }

    /**
     * The delete request starts once the confirmation has finished closing, which is not
     * synchronous with the click. Waiting for the close (not for stability, which the open
     * request would hold back) lets the spec see the request.
     */
    async function answer(dialog: HTMLElement, label: 'Delete' | 'Cancel'): Promise<void> {
      const closed = firstValueFrom(TestBed.inject(MatDialog).afterAllClosed);
      buttonWithText(dialog, label).click();
      await closed;
    }

    it('deletes after confirmation', async () => {
      const el = await open();

      const dialog = await askToDelete(el, 'Pay rent');
      expect(dialog.textContent).toContain('Pay rent');
      await answer(dialog, 'Delete');
      const req = http.expectOne('/api/tasks/t-3');
      expect(req.request.method).toBe('DELETE');
      req.flush(null, { status: 204, statusText: 'No Content' });
      await settle();

      expect(titles(el)).toEqual(['Write report', 'Book flights']);
      expect(overlayText()).toContain('Task deleted');
    });

    it('does nothing when the confirmation is cancelled', async () => {
      const el = await open();

      await answer(await askToDelete(el, 'Pay rent'), 'Cancel');
      await settle();

      http.expectNone('/api/tasks/t-3');
      expect(titles(el)).toHaveLength(3);
    });

    it('removes a task that was already deleted elsewhere and says so', async () => {
      const el = await open();

      await answer(await askToDelete(el, 'Pay rent'), 'Delete');
      http
        .expectOne('/api/tasks/t-3')
        .flush({ status: 404, title: 'Not found' }, { status: 404, statusText: 'Not Found' });
      await settle();

      expect(titles(el)).toEqual(['Write report', 'Book flights']);
      expect(overlayText()).toContain('This task no longer exists.');
    });
  });

  describe('isolation between sessions', () => {
    const ANA = [makeTask('a-1', { title: "Ana's secret plan" })];
    const BRUNO = [makeTask('b-1', { title: "Bruno's errand" })];

    it('creates a new store each time the page opens and shows nothing old while loading', async () => {
      await open('/tasks', ANA);
      const first = pageStore();

      await harness.navigateByUrl('/login');
      await harness.navigateByUrl('/tasks', TasksPage);
      const second = pageStore();

      expect(second).not.toBe(first);
      expect(root().textContent).not.toContain("Ana's secret plan");
      expect(root().textContent).toContain('Loading tasks');

      http.expectOne('/api/tasks').flush(BRUNO);
      await settle();
      expect(titles(root())).toEqual(["Bruno's errand"]);
    });

    it('gives its dialogs the same store instance', async () => {
      const el = await open();

      action(el, 'Write report', 'Edit').click();
      await settle();
      const dialogRef = TestBed.inject(MatDialog).openDialogs[0];

      expect(dialogRef.componentRef?.injector.get(TasksStore)).toBe(pageStore());
    });

    it.each([
      ['edit form', 'Edit'],
      ['delete confirmation', 'Delete'],
    ])('closes an open %s when the page is left', async (_name, label) => {
      const el = await open('/tasks', ANA);
      action(el, "Ana's secret plan", label).click();
      await settle();
      expect(openDialogElements()).toHaveLength(1);

      await harness.navigateByUrl('/login');
      await settle();

      expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
      expect(openDialogElements()).toHaveLength(0);
      expect(overlayText()).not.toContain("Ana's secret plan");
    });
  });
});
