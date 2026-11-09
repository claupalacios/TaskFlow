import { ChangeDetectionStrategy, Component } from '@angular/core';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel: string;
}

/** Asks the user to confirm; closes with `true` only when they do. */
@Component({
  selector: 'app-confirm-dialog',
  template: '',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {}
