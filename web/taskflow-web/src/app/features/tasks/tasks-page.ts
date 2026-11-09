import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Landing page for signed-in users. The task list itself arrives in the next milestone. */
@Component({
  selector: 'app-tasks-page',
  template: '<h1 class="tasks-title">My tasks</h1>',
  styles: '.tasks-title { margin: 24px 16px; font: var(--mat-sys-headline-small); }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TasksPage {}
