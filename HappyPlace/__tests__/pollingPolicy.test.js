import {
    messagesPollingInterval,
    listPollingInterval,
    membersPollingInterval,
    helpPollingInterval,
    unreadBadgePollingInterval,
} from '../src/utils/pollingPolicy';

describe('messagesPollingInterval', () => {
    test('stretches to thirty seconds when the socket is healthy', () => {
        expect(messagesPollingInterval(true)).toBe(30000);
    });

    test('falls back to two seconds when the socket is down', () => {
        expect(messagesPollingInterval(false)).toBe(2000);
    });
});

describe('listPollingInterval', () => {
    test('stops entirely when the screen is not focused', () => {
        expect(listPollingInterval(true, false)).toBe(0);
        expect(listPollingInterval(false, false)).toBe(0);
    });

    test('stretches to thirty seconds when focused with a healthy socket', () => {
        expect(listPollingInterval(true, true)).toBe(30000);
    });

    test('falls back to five seconds when focused with the socket down', () => {
        expect(listPollingInterval(false, true)).toBe(5000);
    });
});

describe('membersPollingInterval', () => {
    test('stops entirely when the screen is not focused', () => {
        expect(membersPollingInterval(true, false)).toBe(0);
        expect(membersPollingInterval(false, false)).toBe(0);
    });

    test('stretches to thirty seconds when focused with a healthy socket', () => {
        expect(membersPollingInterval(true, true)).toBe(30000);
    });

    test('falls back to three seconds when focused with the socket down', () => {
        expect(membersPollingInterval(false, true)).toBe(3000);
    });
});

describe('helpPollingInterval', () => {
    test('stretches to thirty seconds when the socket is healthy', () => {
        expect(helpPollingInterval(true)).toBe(30000);
    });

    test('falls back to three seconds when the socket is down', () => {
        expect(helpPollingInterval(false)).toBe(3000);
    });
});

describe('unreadBadgePollingInterval', () => {
    test('stretches to sixty seconds when the socket is healthy', () => {
        expect(unreadBadgePollingInterval(true)).toBe(60000);
    });

    test('falls back to fifteen seconds when the socket is down', () => {
        expect(unreadBadgePollingInterval(false)).toBe(15000);
    });
});