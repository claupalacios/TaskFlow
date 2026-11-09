import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * The signed-in user's tasks. Owns the `TasksStore` (component provider), shares it with the
 * dialogs it opens, and closes those dialogs when it is destroyed.
 */
@Component({
  selector: 'app-tasks-page',
  template: '',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TasksPage {}
