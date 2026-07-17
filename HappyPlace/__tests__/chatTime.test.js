import { parseUtcInstant, formatMessageTime, localDateKey, localDateKeyForDaysAgo, formatDayHeader } from '../src/utils/chatTime';

describe('parseUtcInstant', () => {
    it('treats a timestamp without timezone info as UTC', () => {
        expect(parseUtcInstant('2026-07-15T01:05:00').getTime()).toBe(Date.parse('2026-07-15T01:05:00Z'));
    });

    it('keeps an explicit Z suffix as UTC', () => {
        expect(parseUtcInstant('2026-07-15T01:05:00Z').getTime()).toBe(Date.parse('2026-07-15T01:05:00Z'));
    });

    it('keeps an explicit offset suffix', () => {
        expect(parseUtcInstant('2026-07-15T01:05:00+02:00').getTime()).toBe(Date.parse('2026-07-15T01:05:00+02:00'));
    });

    it('keeps fractional seconds with a Z suffix', () => {
        expect(parseUtcInstant('2026-07-15T01:05:00.1234567Z').getTime()).toBe(Date.parse('2026-07-15T01:05:00.123Z'));
    });

    it('passes Date instances through unchanged', () => {
        const instant = new Date(1752541500000);
        expect(parseUtcInstant(instant)).toBe(instant);
    });

    it('returns an invalid date for empty input', () => {
        expect(Number.isNaN(parseUtcInstant('').getTime())).toBe(true);
    });
});

describe('formatMessageTime', () => {
    it('formats a suffixless timestamp and its Z twin identically', () => {
        expect(formatMessageTime('2026-07-15T01:05:00')).toBe(formatMessageTime('2026-07-15T01:05:00Z'));
    });

    it('renders the device local wall clock time', () => {
        const epochMs = Date.parse('2026-07-15T01:05:00Z');
        const expected = new Date(epochMs).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true }).toLowerCase();
        expect(formatMessageTime('2026-07-15T01:05:00Z')).toBe(expected);
    });

    it('returns an empty string for unparseable input', () => {
        expect(formatMessageTime('not a date')).toBe('');
    });
});

describe('localDateKey', () => {
    it('keys by the local calendar date', () => {
        expect(localDateKey(new Date(2026, 6, 15, 20, 5))).toBe('2026-07-15');
    });

    it('zero pads months and days', () => {
        expect(localDateKey(new Date(2026, 0, 5, 0, 0))).toBe('2026-01-05');
    });
});

describe('localDateKeyForDaysAgo', () => {
    it('computes today and yesterday on the local calendar', () => {
        const now = new Date();
        expect(localDateKeyForDaysAgo(0)).toBe(localDateKey(now));
        expect(localDateKeyForDaysAgo(1)).toBe(localDateKey(new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1)));
    });
});

describe('formatDayHeader', () => {
    it('renders headers from the local calendar date, never a UTC parse', () => {
        const expected = new Date(2026, 6, 15).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
        expect(formatDayHeader('2026-07-15')).toBe(expected);
        expect(formatDayHeader('2026-07-15')).toContain('15');
        expect(formatDayHeader('2026-07-15')).toContain('Jul');
    });
});