import { TASK_STATUSES, TaskStatus } from './task.models';

/** Reads the `status` query parameter; anything that is not an exact status means "all". */
export function parseStatusFilter(raw: string | null | undefined): TaskStatus | null {
  return TASK_STATUSES.find((status) => status === raw) ?? null;
}
