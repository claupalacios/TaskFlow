import { parseStatusFilter } from './status-filter';
import { TASK_STATUSES } from './task.models';

describe('parseStatusFilter', () => {
  it.each(TASK_STATUSES)('reads %s', (status) => {
    expect(parseStatusFilter(status)).toBe(status);
  });

  it.each([null, undefined, '', 'pending', 'Done', 'Pending '])('treats %s as no filter', (raw) => {
    expect(parseStatusFilter(raw)).toBeNull();
  });
});
