/** Small DOM helpers shared by component specs. */
export function query<T extends Element = HTMLElement>(root: ParentNode, selector: string): T {
  const element = root.querySelector<T>(selector);
  if (!element) {
    throw new Error(`No element matches "${selector}"`);
  }
  return element;
}

export function control(root: ParentNode, name: string): HTMLInputElement {
  return query<HTMLInputElement>(root, `input[formcontrolname="${name}"]`);
}

export function textArea(root: ParentNode, name: string): HTMLTextAreaElement {
  return query<HTMLTextAreaElement>(root, `textarea[formcontrolname="${name}"]`);
}

/** Types into an input the way a user would, then leaves the field. */
export function typeInto(input: HTMLInputElement | HTMLTextAreaElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('blur'));
}

export function submitButton(root: ParentNode): HTMLButtonElement {
  return query<HTMLButtonElement>(root, 'button[type="submit"]');
}

/** The first button whose trimmed text contains `text`. */
export function buttonWithText(root: ParentNode, text: string): HTMLButtonElement {
  const button = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find((b) =>
    b.textContent?.trim().includes(text),
  );
  if (!button) {
    throw new Error(`No button contains "${text}"`);
  }
  return button;
}

/** Dialogs currently in the CDK overlay (outside the component tree). */
export function openDialogElements(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>('mat-dialog-container'));
}

/** Text currently rendered in the CDK overlay: dialogs, menus and snack bars. */
export function overlayText(): string {
  return document.querySelector('.cdk-overlay-container')?.textContent ?? '';
}
