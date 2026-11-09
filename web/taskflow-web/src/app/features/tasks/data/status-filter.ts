import { notImplemented } from '../../../core/not-implemented';
import { TaskStatus } from './task.models';

/** Reads the `status` query parameter; anything that is not an exact status means "all". */
export function parseStatusFilter(raw: string | null | undefined): TaskStatus | null {
  return notImplemented(raw);
}
