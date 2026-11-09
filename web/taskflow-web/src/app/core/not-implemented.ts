/** RED-phase placeholder: removed when the behaviour is implemented. */
export function notImplemented(...args: unknown[]): never {
  throw new Error(`Not implemented (${args.length} argument(s))`);
}
