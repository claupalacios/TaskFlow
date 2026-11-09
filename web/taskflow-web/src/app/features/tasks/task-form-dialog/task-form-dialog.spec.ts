import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, Injector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Observable, Subject, of, throwError } from 'rxjs';

import {
  buttonWithText,
  control,
  openDialogElements,
  query,
  submitButton,
  textArea,
  typeInto,
} from '../../../../testing/dom';
import { noMaterialAnimations } from '../../../../testing/material';
import { makeTask } from '../../../../testing/tasks';
import { CreateTaskRequest, TaskResponse, UpdateTaskRequest } from '../data/task.models';
import { TasksStore } from '../data/tasks-store';
import { TaskFormDialog, TaskFormDialogData } from './task-form-dialog';

const EXISTING = makeTask('t-1', {
  title: 'Write report',
  description: 'Quarterly numbers',
  status: 'InProgress',
  dueDate: '2026-11-20',
});

function apiError(status: number, error: unknown): Observable<never> {
  return throwError(() => new HttpErrorResponse({ status, url: '/api/tasks', error }));
}

describe('TaskFormDialog', () => {
  let create: ReturnType<typeof vi.fn<(request: CreateTaskRequest) => Observable<TaskResponse>>>;
  let update: ReturnType<
    typeof vi.fn<(id: string, request: UpdateTaskRequest) => Observable<TaskResponse>>
  >;
  let result: unknown;

  beforeEach(() => {
    create = vi.fn(() => of(makeTask('t-9')));
    update = vi.fn(() => of(EXISTING));
    TestBed.configureTestingModule({ providers: [noMaterialAnimations] });
  });

  const settle = () => TestBed.inject(ApplicationRef).whenStable();

  /** Opens the dialog the way TasksPage does: the store comes from the opener's injector. */
  async function open(data: TaskFormDialogData): Promise<HTMLElement> {
    result = 'still open';
    const injector = Injector.create({
      providers: [{ provide: TasksStore, useValue: { create, update } }],
      parent: TestBed.inject(Injector),
    });
    TestBed.inject(MatDialog)
      .open(TaskFormDialog, { data, injector })
      .afterClosed()
      .subscribe((r) => (result = r));
    await settle();
    return openDialogElements()[0];
  }

  async function submit(dialog: HTMLElement): Promise<void> {
    submitButton(dialog).click();
    await settle();
  }

  describe('creating', () => {
    it('starts empty, without a status field', async () => {
      const dialog = await open({ task: null });

      expect(dialog.textContent).toContain('New task');
      expect(control(dialog, 'title').value).toBe('');
      expect(textArea(dialog, 'description').value).toBe('');
      expect(control(dialog, 'dueDate').value).toBe('');
      expect(dialog.querySelector('[formcontrolname="status"]')).toBeNull();
    });

    it('creates the task with a trimmed title and an ISO due date, then closes', async () => {
      const dialog = await open({ task: null });

      typeInto(control(dialog, 'title'), '  Write report  ');
      typeInto(textArea(dialog, 'description'), 'Quarterly numbers');
      typeInto(control(dialog, 'dueDate'), '11/20/2026');
      await submit(dialog);

      expect(create).toHaveBeenCalledExactlyOnceWith({
        title: 'Write report',
        description: 'Quarterly numbers',
        dueDate: '2026-11-20',
      });
      expect(result).toBe('created');
      expect(openDialogElements()).toHaveLength(0);
    });

    it('sends an empty description and no due date when they are left blank', async () => {
      const dialog = await open({ task: null });

      typeInto(control(dialog, 'title'), 'Write report');
      await submit(dialog);

      expect(create).toHaveBeenCalledExactlyOnceWith({
        title: 'Write report',
        description: '',
        dueDate: null,
      });
    });

    it.each([
      ['empty', ''],
      ['only spaces', '   '],
      ['longer than 200 characters', 'x'.repeat(201)],
    ])('does not save a title that is %s', async (_rule, title) => {
      const dialog = await open({ task: null });

      typeInto(control(dialog, 'title'), title);
      await submit(dialog);

      expect(create).not.toHaveBeenCalled();
      expect(dialog.querySelectorAll('mat-error').length).toBeGreaterThan(0);
      expect(result).toBe('still open');
    });

    it('does not save a description longer than 2000 characters', async () => {
      const dialog = await open({ task: null });

      typeInto(control(dialog, 'title'), 'Write report');
      typeInto(textArea(dialog, 'description'), 'x'.repeat(2001));
      await submit(dialog);

      expect(create).not.toHaveBeenCalled();
      expect(dialog.querySelectorAll('mat-error').length).toBeGreaterThan(0);
    });

    it('disables the submit button while saving', async () => {
      const pending = new Subject<TaskResponse>();
      create.mockReturnValue(pending);
      const dialog = await open({ task: null });

      typeInto(control(dialog, 'title'), 'Write report');
      await submit(dialog);
      expect(submitButton(dialog).disabled).toBe(true);

      pending.next(makeTask('t-9'));
      pending.complete();
      await settle();
      expect(result).toBe('created');
    });

    it('shows server errors and stays open so the user can fix them', async () => {
      create.mockReturnValue(
        apiError(400, {
          status: 400,
          title: 'One or more validation errors occurred.',
          errors: { Title: ['The server rejected this title.'] },
        }),
      );
      const dialog = await open({ task: null });

      typeInto(control(dialog, 'title'), 'Write report');
      await submit(dialog);

      expect(query(dialog, '[role="alert"]').textContent).toContain(
        'One or more validation errors occurred.',
      );
      expect(control(dialog, 'title').closest('mat-form-field')?.textContent).toContain(
        'The server rejected this title.',
      );
      expect(submitButton(dialog).disabled).toBe(false);
      expect(result).toBe('still open');
    });
  });

  describe('editing', () => {
    it('starts with the task values and a status field', async () => {
      const dialog = await open({ task: EXISTING });

      expect(dialog.textContent).toContain('Edit task');
      expect(control(dialog, 'title').value).toBe('Write report');
      expect(textArea(dialog, 'description').value).toBe('Quarterly numbers');
      expect(control(dialog, 'dueDate').value).toBe('11/20/2026');
      expect(query(dialog, 'mat-select[formcontrolname="status"]').textContent).toContain(
        'In progress',
      );
    });

    it('saves the full task, keeping the fields the user did not touch', async () => {
      const dialog = await open({ task: EXISTING });

      typeInto(control(dialog, 'title'), 'Write the report');
      await submit(dialog);

      expect(update).toHaveBeenCalledExactlyOnceWith('t-1', {
        title: 'Write the report',
        description: 'Quarterly numbers',
        status: 'InProgress',
        dueDate: '2026-11-20',
      });
      expect(result).toBe('updated');
    });

    it('clears the due date when the user empties it', async () => {
      const dialog = await open({ task: EXISTING });

      typeInto(control(dialog, 'dueDate'), '');
      await submit(dialog);

      expect(update.mock.calls[0][1].dueDate).toBeNull();
    });

    it('closes as gone when the task no longer exists', async () => {
      update.mockReturnValue(apiError(404, { status: 404, title: 'Not found' }));
      const dialog = await open({ task: EXISTING });

      await submit(dialog);

      expect(result).toBe('gone');
      expect(openDialogElements()).toHaveLength(0);
    });
  });

  it('closes without saving when cancelled', async () => {
    const dialog = await open({ task: EXISTING });

    buttonWithText(dialog, 'Cancel').click();
    await settle();

    expect(update).not.toHaveBeenCalled();
    expect(create).not.toHaveBeenCalled();
    expect(result).toBeUndefined();
  });
});
