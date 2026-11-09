import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Observable, map } from 'rxjs';

import { parseApiError } from '../../../core/api/api-error';
import { applyFieldErrors } from '../../../core/api/form-errors';
import { fromIsoDate, toIsoDate } from '../data/due-date';
import { TASK_STATUSES, TASK_STATUS_LABELS, TaskResponse, TaskStatus } from '../data/task.models';
import { TasksStore } from '../data/tasks-store';

/** Mirror the API's TaskItem limits. */
export const TITLE_MAX_LENGTH = 200;
export const DESCRIPTION_MAX_LENGTH = 2000;

export interface TaskFormDialogData {
  /** The task to edit, or null to create one. */
  task: TaskResponse | null;
}

/** How the dialog ended; `undefined` when it was cancelled. */
export type TaskFormResult = 'created' | 'updated' | 'gone';

/** Required already rejects ''; this rejects text made only of spaces, which the API trims away. */
function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value && control.value.trim() === '' ? { blank: true } : null;
}

/**
 * The single create/edit form. Saves through the `TasksStore` of the page that opened it
 * and closes only when the save succeeds (or the task turns out to be gone).
 */
@Component({
  selector: 'app-task-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatSelectModule,
    MatButtonModule,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './task-form-dialog.html',
  styleUrl: './task-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskFormDialog {
  private readonly store = inject(TasksStore);
  private readonly dialogRef = inject<MatDialogRef<TaskFormDialog, TaskFormResult>>(MatDialogRef);
  private readonly task = inject<TaskFormDialogData>(MAT_DIALOG_DATA).task;

  protected readonly editing = this.task !== null;
  protected readonly statuses = TASK_STATUSES;
  protected readonly labels = TASK_STATUS_LABELS;
  protected readonly titleMaxLength = TITLE_MAX_LENGTH;
  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;

  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly form = this.fb.group({
    title: [
      this.task?.title ?? '',
      [Validators.required, notBlank, Validators.maxLength(TITLE_MAX_LENGTH)],
    ],
    description: [this.task?.description ?? '', Validators.maxLength(DESCRIPTION_MAX_LENGTH)],
    dueDate: this.fb.control<Date | null>(
      this.task?.dueDate ? fromIsoDate(this.task.dueDate) : null,
    ),
    status: this.fb.control<TaskStatus>(this.task?.status ?? 'Pending'),
  });

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected cancel(): void {
    this.dialogRef.close();
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.save().subscribe({
      next: (result) => this.dialogRef.close(result),
      error: (e: unknown) => {
        // Editing a task deleted elsewhere: the store already dropped it; let the page say so.
        if (this.editing && e instanceof HttpErrorResponse && e.status === 404) {
          this.dialogRef.close('gone');
          return;
        }
        const apiError = parseApiError(e);
        applyFieldErrors(this.form, apiError.fieldErrors);
        this.error.set(apiError.message);
        this.saving.set(false);
      },
    });
  }

  private save(): Observable<TaskFormResult> {
    const { title, description, dueDate, status } = this.form.getRawValue();
    const fields = {
      title: title.trim(),
      description,
      dueDate: dueDate ? toIsoDate(dueDate) : null,
    };

    return this.task
      ? this.store.update(this.task.id, { ...fields, status }).pipe(map(() => 'updated' as const))
      : this.store.create(fields).pipe(map(() => 'created' as const));
  }
}
