import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';

import { buttonWithText, openDialogElements } from '../../../../testing/dom';
import { noMaterialAnimations } from '../../../../testing/material';
import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog';

const DATA: ConfirmDialogData = {
  title: 'Delete task?',
  message: '"Write report" will be deleted.',
  confirmLabel: 'Delete',
};

describe('ConfirmDialog', () => {
  let result: unknown;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [noMaterialAnimations] });
  });

  const settle = () => TestBed.inject(ApplicationRef).whenStable();

  async function open(): Promise<HTMLElement> {
    result = 'still open';
    TestBed.inject(MatDialog)
      .open(ConfirmDialog, { data: DATA })
      .afterClosed()
      .subscribe((r) => (result = r));
    await settle();
    return openDialogElements()[0];
  }

  it('shows the title and the message', async () => {
    const dialog = await open();

    expect(dialog.textContent).toContain('Delete task?');
    expect(dialog.textContent).toContain('"Write report" will be deleted.');
  });

  it('closes with true when confirmed', async () => {
    const dialog = await open();

    buttonWithText(dialog, 'Delete').click();
    await settle();

    expect(result).toBe(true);
    expect(openDialogElements()).toHaveLength(0);
  });

  it('closes with false when cancelled', async () => {
    const dialog = await open();

    buttonWithText(dialog, 'Cancel').click();
    await settle();

    expect(result).toBe(false);
  });
});
