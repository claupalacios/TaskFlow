import { notImplemented } from '../../../core/not-implemented';
import { TaskResponse } from './task.models';

/** The local calendar date of `date` as `YYYY-MM-DD` (never shifted through UTC). */
export function toIsoDate(date: Date): string {
  return notImplemented(date);
}

/** Local midnight of a `YYYY-MM-DD` date, or null when the text is not a real date. */
export function fromIsoDate(iso: string): Date | null {
  return notImplemented(iso);
}

/** Past its due date (today is not overdue) and not completed. */
export function isOverdue(
  task: Pick<TaskResponse, 'dueDate' | 'status'>,
  today = new Date(),
): boolean {
  return notImplemented(task, today);
}
