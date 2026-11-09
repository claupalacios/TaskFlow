import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { TaskResponse, TaskStatus } from '../data/task.models';

/** One task in the list. Presentational: it only reports what the user asked for. */
@Component({
  selector: 'app-task-item',
  template: '',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskItem {
  readonly task = input.required<TaskResponse>();

  readonly edit = output<void>();
  readonly remove = output<void>();
  readonly statusChange = output<TaskStatus>();
}
