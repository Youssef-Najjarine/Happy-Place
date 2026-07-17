const timezoneSuffixPattern = /(Z|z|[+-]\d{2}:?\d{2})$/;

export function parseUtcInstant(value) {
    if (value instanceof Date) return value;
    if (typeof value !== 'string' || value.length === 0) return new Date(NaN);
    const normalizedValue = timezoneSuffixPattern.test(value) ? value : value + 'Z';
    return new Date(normalizedValue);
}

export function formatMessageTime(value) {
    const instant = parseUtcInstant(value);
    if (Number.isNaN(instant.getTime())) return '';
    return instant.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true }).toLowerCase();
}

export function localDateKey(value) {
    const instant = parseUtcInstant(value);
    if (Number.isNaN(instant.getTime())) return '';
    const paddedMonth = String(instant.getMonth() + 1).padStart(2, '0');
    const paddedDay = String(instant.getDate()).padStart(2, '0');
    return instant.getFullYear() + '-' + paddedMonth + '-' + paddedDay;
}

export function localDateKeyForDaysAgo(daysAgo) {
    const now = new Date();
    return localDateKey(new Date(now.getFullYear(), now.getMonth(), now.getDate() - daysAgo));
}

export function formatDayHeader(dateKey) {
    const keyParts = dateKey.split('-');
    const localDate = new Date(Number(keyParts[0]), Number(keyParts[1]) - 1, Number(keyParts[2]));
    return localDate.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
}