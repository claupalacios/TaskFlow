/** Small DOM helpers shared by component specs. */
export function query<T extends Element = HTMLElement>(root: Element, selector: string): T {
  const element = root.querySelector<T>(selector);
  if (!element) {
    throw new Error(`No element matches "${selector}"`);
  }
  return element;
}

export function control(root: Element, name: string): HTMLInputElement {
  return query<HTMLInputElement>(root, `input[formcontrolname="${name}"]`);
}

/** Types into an input the way a user would, then leaves the field. */
export function typeInto(input: HTMLInputElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('blur'));
}

export function submitButton(root: Element): HTMLButtonElement {
  return query<HTMLButtonElement>(root, 'button[type="submit"]');
}
