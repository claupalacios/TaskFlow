import { ComponentFixture, TestBed } from '@angular/core/testing';

import { query } from '../../../../testing/dom';
import { noMaterialAnimations } from '../../../../testing/material';
import { makeTask } from '../../../../testing/tasks';
import { TaskResponse, TaskStatus } from '../data/task.models';
import { TaskItem } from './task-item';

describe('TaskItem', () => {
  let fixture: ComponentFixture<TaskItem>;

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 10, 9, 10, 0));
    TestBed.configureTestingModule({ providers: [noMaterialAnimations] });
  });

  afterEach(() => vi.useRealTimers());

  async function render(task: TaskResponse): Promise<HTMLElement> {
    fixture = TestBed.createComponent(TaskItem);
    fixture.componentRef.setInput('task', task);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  const button = (root: HTMLElement, label: string) =>
    query<HTMLButtonElement>(root, `button[aria-label^="${label}"]`);

  it('shows the title as a heading, the description, the status and the due date', async () => {
    const root = await render(
      makeTask('t-1', {
        title: 'Write report',
        description: 'Quarterly numbers',
        status: 'InProgress',
        dueDate: '2026-11-20',
      }),
    );

    expect(query(root, 'h3').textContent?.trim()).toBe('Write report');
    expect(root.textContent).toContain('Quarterly numbers');
    expect(root.textContent).toContain('In progress');
    expect(root.textContent).toContain('Nov 20, 2026');
  });

  it('shows no due date when there is none', async () => {
    const root = await render(makeTask('t-1', { dueDate: null }));

    expect(root.textContent).not.toContain('Due');
  });

  it('flags an overdue task', async () => {
    const root = await render(makeTask('t-1', { dueDate: '2026-11-08', status: 'Pending' }));

    expect(root.textContent).toContain('Overdue');
  });

  it('does not flag a completed task as overdue', async () => {
    const root = await render(makeTask('t-1', { dueDate: '2026-11-08', status: 'Completed' }));

    expect(root.textContent).not.toContain('Overdue');
  });

  it('names the task in the action labels', async () => {
    const root = await render(makeTask('t-1', { title: 'Write report' }));

    expect(button(root, 'Edit').getAttribute('aria-label')).toBe('Edit Write report');
    expect(button(root, 'Delete').getAttribute('aria-label')).toBe('Delete Write report');
    expect(button(root, 'Change status').getAttribute('aria-label')).toBe(
      'Change status of Write report',
    );
  });

  it('reports edit and delete', async () => {
    const root = await render(makeTask('t-1'));
    const edit = vi.fn();
    const remove = vi.fn();
    fixture.componentInstance.edit.subscribe(edit);
    fixture.componentInstance.remove.subscribe(remove);

    button(root, 'Edit').click();
    button(root, 'Delete').click();

    expect(edit).toHaveBeenCalledOnce();
    expect(remove).toHaveBeenCalledOnce();
  });

  it('offers the other statuses and reports the chosen one', async () => {
    const root = await render(makeTask('t-1', { status: 'Pending' }));
    const statusChange = vi.fn<(status: TaskStatus) => void>();
    fixture.componentInstance.statusChange.subscribe(statusChange);

    button(root, 'Change status').click();
    await fixture.whenStable();
    const options = Array.from(document.querySelectorAll<HTMLButtonElement>('[mat-menu-item]'));

    expect(options.map((o) => o.textContent?.trim())).toEqual(['In progress', 'Completed']);
    options[1].click();
    expect(statusChange).toHaveBeenCalledExactlyOnceWith('Completed');
  });
});
