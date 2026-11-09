/** Shapes of the Tasks API (`/api/tasks`). `dueDate` is a calendar date (`YYYY-MM-DD`). */
export const TASK_STATUSES = ['Pending', 'InProgress', 'Completed'] as const;

export type TaskStatus = (typeof TASK_STATUSES)[number];

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  Pending: 'Pending',
  InProgress: 'In progress',
  Completed: 'Completed',
};

export interface TaskResponse {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  dueDate: string | null;
}

/** A full replacement: every field is sent, including the ones that did not change. */
export interface UpdateTaskRequest extends CreateTaskRequest {
  status: TaskStatus;
}

export type TaskCounts = Record<TaskStatus, number> & { all: number };
