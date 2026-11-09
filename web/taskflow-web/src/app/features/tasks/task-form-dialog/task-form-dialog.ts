import { ChangeDetectionStrategy, Component } from '@angular/core';

import { TaskResponse } from '../data/task.models';

export interface TaskFormDialogData {
  /** The task to edit, or null to create one. */
  task: TaskResponse | null;
}

/** How the dialog ended; `undefined` when it was cancelled. */
export type TaskFormResult = 'created' | 'updated' | 'gone';

/**
 * The single create/edit form. Saves through the `TasksStore` of the page that opened it
 * and closes only when the save succeeds (or the task turns out to be gone).
 */
@Component({
  selector: 'app-task-form-dialog',
  template: '',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskFormDialog {}
