import { TaskResponse } from '../app/features/tasks/data/task.models';

/** A task as the API returns it; override only what a test cares about. */
export function makeTask(id: string, overrides: Partial<TaskResponse> = {}): TaskResponse {
  return {
    id,
    title: `Task ${id}`,
    description: `About ${id}`,
    status: 'Pending',
    dueDate: '2026-11-20',
    createdAt: '2026-11-09T12:00:00+00:00',
    updatedAt: '2026-11-09T12:00:00+00:00',
    ...overrides,
  };
}
