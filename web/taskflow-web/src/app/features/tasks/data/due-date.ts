import { TaskResponse } from './task.models';

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/;

/** The local calendar date of `date` as `YYYY-MM-DD` (never shifted through UTC). */
export function toIsoDate(date: Date): string {
  const year = String(date.getFullYear()).padStart(4, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/** Local midnight of a `YYYY-MM-DD` date, or null when the text is not a real date. */
export function fromIsoDate(iso: string): Date | null {
  const match = ISO_DATE.exec(iso);
  if (!match) {
    return null;
  }
  const [year, month, day] = match.slice(1).map(Number);
  const date = new Date(year, month - 1, day);
  // Date rolls invalid days over (Feb 30 → Mar 2); a round trip catches that.
  return toIsoDate(date) === iso ? date : null;
}

/** Past its due date (today is not overdue) and not completed. */
export function isOverdue(
  task: Pick<TaskResponse, 'dueDate' | 'status'>,
  today = new Date(),
): boolean {
  // ISO dates compare correctly as strings.
  return task.status !== 'Completed' && task.dueDate !== null && task.dueDate < toIsoDate(today);
}
