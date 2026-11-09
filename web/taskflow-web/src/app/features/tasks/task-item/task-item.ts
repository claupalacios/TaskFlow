import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

import { fromIsoDate, isOverdue } from '../data/due-date';
import { TASK_STATUSES, TASK_STATUS_LABELS, TaskResponse, TaskStatus } from '../data/task.models';

/** One task in the list. Presentational: it only reports what the user asked for. */
@Component({
  selector: 'app-task-item',
  imports: [DatePipe, MatButtonModule, MatIconModule, MatMenuModule],
  templateUrl: './task-item.html',
  styleUrl: './task-item.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskItem {
  readonly task = input.required<TaskResponse>();

  readonly edit = output<void>();
  readonly remove = output<void>();
  readonly statusChange = output<TaskStatus>();

  protected readonly labels = TASK_STATUS_LABELS;
  protected readonly dueDate = computed(() => {
    const dueDate = this.task().dueDate;
    return dueDate ? fromIsoDate(dueDate) : null;
  });
  protected readonly overdue = computed(() => isOverdue(this.task()));
  protected readonly otherStatuses = computed(() =>
    TASK_STATUSES.filter((status) => status !== this.task().status),
  );
}
