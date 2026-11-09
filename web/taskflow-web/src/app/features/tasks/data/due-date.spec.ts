import { fromIsoDate, isOverdue, toIsoDate } from './due-date';
import { TaskStatus } from './task.models';

describe('toIsoDate', () => {
  it('pads month and day', () => {
    expect(toIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('keeps the local date late at night instead of shifting through UTC', () => {
    expect(toIsoDate(new Date(2026, 10, 9, 23, 30))).toBe('2026-11-09');
  });

  it('keeps the local date just after midnight', () => {
    expect(toIsoDate(new Date(2026, 10, 9, 0, 15))).toBe('2026-11-09');
  });
});

describe('fromIsoDate', () => {
  it('returns local midnight of that date', () => {
    const date = fromIsoDate('2026-11-09');

    expect(date).not.toBeNull();
    expect([date?.getFullYear(), date?.getMonth(), date?.getDate(), date?.getHours()]).toEqual([
      2026, 10, 9, 0,
    ]);
  });

  it('round-trips with toIsoDate', () => {
    const date = fromIsoDate('2026-02-28');

    expect(date && toIsoDate(date)).toBe('2026-02-28');
  });

  it.each(['', 'not a date', '2026-13-01', '2026-02-30', '2026-1-5', '2026-11-09T10:00:00'])(
    'returns null for %s',
    (iso) => {
      expect(fromIsoDate(iso)).toBeNull();
    },
  );
});

describe('isOverdue', () => {
  const today = new Date(2026, 10, 9, 10, 0);
  const task = (dueDate: string | null, status: TaskStatus = 'Pending') => ({ dueDate, status });

  it.each<TaskStatus>(['Pending', 'InProgress'])(
    'is overdue when %s and due yesterday',
    (status) => {
      expect(isOverdue(task('2026-11-08', status), today)).toBe(true);
    },
  );

  it('is not overdue once completed', () => {
    expect(isOverdue(task('2026-11-08', 'Completed'), today)).toBe(false);
  });

  it('is not overdue on the due date itself', () => {
    expect(isOverdue(task('2026-11-09'), today)).toBe(false);
  });

  it('is not overdue before the due date', () => {
    expect(isOverdue(task('2026-11-10'), today)).toBe(false);
  });

  it('is not overdue without a due date', () => {
    expect(isOverdue(task(null), today)).toBe(false);
  });
});
