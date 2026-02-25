const MONTHS = [
  "Hearthbound", "Harrowe", "Rainhold", "Lambstide", "Brighthold", "Midsomme",
  "Hayward", "Windharrow", "Wainrest", "Duskfall", "Yulecroft", "Yearwane",
] as const;

const WEEK_DAYS = [
  "Crownday", "Turnday", "Midmark", "Aldersday", "Hammerday", "Graceday",
] as const;

const DAYS_PER_MONTH = 30;
const MONTHS_PER_YEAR = 12;
const DAYS_PER_WEEK = 6;
const MONTH_DAYS_PER_YEAR = DAYS_PER_MONTH * MONTHS_PER_YEAR; // 360
const DAYS_PER_YEAR = MONTH_DAYS_PER_YEAR + 5; // 365

export interface CalendarDate {
  year: number;
  month: string | null;
  dayOfMonth: number;
  weekDay: string | null;
}

export function getDate(day: number): CalendarDate {
  const d = day - 1;
  const year = Math.floor(d / DAYS_PER_YEAR) + 1;
  const dayInYear = d % DAYS_PER_YEAR;

  if (dayInYear >= MONTH_DAYS_PER_YEAR) {
    return { year, month: null, dayOfMonth: dayInYear - MONTH_DAYS_PER_YEAR + 1, weekDay: null };
  }

  return {
    year,
    month: MONTHS[Math.floor(dayInYear / DAYS_PER_MONTH)],
    dayOfMonth: (dayInYear % DAYS_PER_MONTH) + 1,
    weekDay: WEEK_DAYS[dayInYear % DAYS_PER_WEEK],
  };
}

export function formatDateTime(day: number, time: string): string {
  const d = getDate(day);
  if (!d.month) return `${time} — Hollow Day ${d.dayOfMonth}, Year ${d.year}`;
  return `${time} of ${d.weekDay}, ${d.dayOfMonth} ${d.month}`;
}
