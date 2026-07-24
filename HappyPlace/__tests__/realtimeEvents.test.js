import {
    CHAT_GROUP_CHANGED_EVENT,
    FRIENDS_CHANGED_EVENT,
    HELP_CHANGED_EVENT,
    MESSAGES_KIND,
    MEMBERSHIP_KIND,
    CHAT_GROUPS_MEMBERSHIP_TAGS,
    CHAT_GROUPS_MESSAGES_TAGS,
    FRIENDS_TAGS,
    buildInvalidationsForEvent,
    nextReconnectDelayMs,
    shouldResyncOnForeground,
    createChatGroupListenerRegistry,
    createSubscriptionCounter,
} from '../src/utils/realtimeEvents';

describe('buildInvalidationsForEvent', () => {
    test('membership chat group event invalidates list and members and carries a membership notification', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 'g1', kind: MEMBERSHIP_KIND });
        expect(result.chatGroupsTags).toEqual(['ChatGroupList', 'ChatGroupMembers']);
        expect(result.friendsTags).toEqual([]);
        expect(result.helpTick).toBe(false);
        expect(result.chatGroupNotification).toEqual({ chatGroupId: 'g1', kind: MEMBERSHIP_KIND });
    });

    test('messages chat group event invalidates only the list and carries a messages notification', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 'g1', kind: MESSAGES_KIND });
        expect(result.chatGroupsTags).toEqual(['ChatGroupList']);
        expect(result.chatGroupNotification).toEqual({ chatGroupId: 'g1', kind: MESSAGES_KIND });
    });

    test('unknown kind is treated as membership', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 'g1', kind: 'mystery' });
        expect(result.chatGroupsTags).toEqual(['ChatGroupList', 'ChatGroupMembers']);
        expect(result.chatGroupNotification).toEqual({ chatGroupId: 'g1', kind: MEMBERSHIP_KIND });
    });

    test('missing kind is treated as membership', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 'g1' });
        expect(result.chatGroupsTags).toEqual(['ChatGroupList', 'ChatGroupMembers']);
        expect(result.chatGroupNotification).toEqual({ chatGroupId: 'g1', kind: MEMBERSHIP_KIND });
    });

    test('null payload still invalidates membership tags without a notification', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, null);
        expect(result.chatGroupsTags).toEqual(['ChatGroupList', 'ChatGroupMembers']);
        expect(result.chatGroupNotification).toBeNull();
    });

    test('missing chat group id yields no notification', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { kind: MESSAGES_KIND });
        expect(result.chatGroupsTags).toEqual(['ChatGroupList']);
        expect(result.chatGroupNotification).toBeNull();
    });

    test('empty string chat group id yields no notification', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: '', kind: MESSAGES_KIND });
        expect(result.chatGroupNotification).toBeNull();
    });

    test('non string chat group id yields no notification', () => {
        const result = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 42, kind: MESSAGES_KIND });
        expect(result.chatGroupNotification).toBeNull();
    });

    test('friends event invalidates every friends tag and nothing else', () => {
        const result = buildInvalidationsForEvent(FRIENDS_CHANGED_EVENT, {});
        expect(result.friendsTags).toEqual(['FriendList', 'IncomingRequests', 'OutgoingRequests', 'UserSearch', 'BlockedList']);
        expect(result.chatGroupsTags).toEqual([]);
        expect(result.helpTick).toBe(false);
        expect(result.chatGroupNotification).toBeNull();
    });

    test('help event only requests a help tick', () => {
        const result = buildInvalidationsForEvent(HELP_CHANGED_EVENT, {});
        expect(result.helpTick).toBe(true);
        expect(result.chatGroupsTags).toEqual([]);
        expect(result.friendsTags).toEqual([]);
        expect(result.chatGroupNotification).toBeNull();
    });

    test('unknown event names produce an empty result', () => {
        const result = buildInvalidationsForEvent('somethingElse', { chatGroupId: 'g1' });
        expect(result).toEqual({ chatGroupsTags: [], friendsTags: [], helpTick: false, chatGroupNotification: null });
    });

    test('null event name produces an empty result', () => {
        const result = buildInvalidationsForEvent(null, null);
        expect(result).toEqual({ chatGroupsTags: [], friendsTags: [], helpTick: false, chatGroupNotification: null });
    });

    test('returned tag arrays are copies so callers cannot corrupt the shared constants', () => {
        const firstResult = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 'g1', kind: MEMBERSHIP_KIND });
        firstResult.chatGroupsTags.push('Corrupted');
        const secondResult = buildInvalidationsForEvent(CHAT_GROUP_CHANGED_EVENT, { chatGroupId: 'g1', kind: MEMBERSHIP_KIND });
        expect(secondResult.chatGroupsTags).toEqual(['ChatGroupList', 'ChatGroupMembers']);
        expect(CHAT_GROUPS_MEMBERSHIP_TAGS).toEqual(['ChatGroupList', 'ChatGroupMembers']);

        const friendsResult = buildInvalidationsForEvent(FRIENDS_CHANGED_EVENT, {});
        friendsResult.friendsTags.push('Corrupted');
        expect(FRIENDS_TAGS).toEqual(['FriendList', 'IncomingRequests', 'OutgoingRequests', 'UserSearch', 'BlockedList']);
        expect(CHAT_GROUPS_MESSAGES_TAGS).toEqual(['ChatGroupList']);
    });
});

describe('nextReconnectDelayMs', () => {
    test('walks the ramp for the first attempts', () => {
        expect(nextReconnectDelayMs(0)).toBe(0);
        expect(nextReconnectDelayMs(1)).toBe(2000);
        expect(nextReconnectDelayMs(2)).toBe(5000);
        expect(nextReconnectDelayMs(3)).toBe(10000);
    });

    test('caps at thirty seconds forever after the ramp', () => {
        expect(nextReconnectDelayMs(4)).toBe(30000);
        expect(nextReconnectDelayMs(5)).toBe(30000);
        expect(nextReconnectDelayMs(500)).toBe(30000);
    });

    test('never returns null so reconnection never gives up', () => {
        for (let retryCount = 0; retryCount < 50; retryCount++) {
            expect(typeof nextReconnectDelayMs(retryCount)).toBe('number');
        }
    });

    test('treats missing or negative counts as an immediate retry', () => {
        expect(nextReconnectDelayMs(null)).toBe(0);
        expect(nextReconnectDelayMs(undefined)).toBe(0);
        expect(nextReconnectDelayMs(-1)).toBe(0);
    });
});

describe('shouldResyncOnForeground', () => {
    test('background to active resyncs', () => {
        expect(shouldResyncOnForeground('background', 'active')).toBe(true);
    });

    test('inactive to active resyncs', () => {
        expect(shouldResyncOnForeground('inactive', 'active')).toBe(true);
    });

    test('unknown previous state to active resyncs', () => {
        expect(shouldResyncOnForeground(undefined, 'active')).toBe(true);
    });

    test('active to active does not resync', () => {
        expect(shouldResyncOnForeground('active', 'active')).toBe(false);
    });

    test('leaving the foreground does not resync', () => {
        expect(shouldResyncOnForeground('active', 'background')).toBe(false);
        expect(shouldResyncOnForeground('active', 'inactive')).toBe(false);
    });

    test('background to inactive does not resync', () => {
        expect(shouldResyncOnForeground('background', 'inactive')).toBe(false);
    });
});

describe('createChatGroupListenerRegistry', () => {
    test('notifies a subscribed listener with the kind', () => {
        const registry = createChatGroupListenerRegistry();
        const received = [];
        registry.subscribe('g1', (kind) => received.push(kind));
        registry.notify('g1', MESSAGES_KIND);
        expect(received).toEqual([MESSAGES_KIND]);
    });

    test('notifies every listener on the same chat group', () => {
        const registry = createChatGroupListenerRegistry();
        const firstReceived = [];
        const secondReceived = [];
        registry.subscribe('g1', (kind) => firstReceived.push(kind));
        registry.subscribe('g1', (kind) => secondReceived.push(kind));
        registry.notify('g1', MEMBERSHIP_KIND);
        expect(firstReceived).toEqual([MEMBERSHIP_KIND]);
        expect(secondReceived).toEqual([MEMBERSHIP_KIND]);
    });

    test('does not notify listeners of other chat groups', () => {
        const registry = createChatGroupListenerRegistry();
        const received = [];
        registry.subscribe('g1', (kind) => received.push(kind));
        registry.notify('g2', MESSAGES_KIND);
        expect(received).toEqual([]);
    });

    test('notifying a chat group with no listeners is a safe no op', () => {
        const registry = createChatGroupListenerRegistry();
        expect(() => registry.notify('missing', MESSAGES_KIND)).not.toThrow();
    });

    test('unsubscribing stops notifications and double unsubscribe is safe', () => {
        const registry = createChatGroupListenerRegistry();
        const received = [];
        const unsubscribe = registry.subscribe('g1', (kind) => received.push(kind));
        unsubscribe();
        unsubscribe();
        registry.notify('g1', MESSAGES_KIND);
        expect(received).toEqual([]);
    });

    test('unsubscribing one listener leaves the others attached', () => {
        const registry = createChatGroupListenerRegistry();
        const firstReceived = [];
        const secondReceived = [];
        const unsubscribeFirst = registry.subscribe('g1', (kind) => firstReceived.push(kind));
        registry.subscribe('g1', (kind) => secondReceived.push(kind));
        unsubscribeFirst();
        registry.notify('g1', MESSAGES_KIND);
        expect(firstReceived).toEqual([]);
        expect(secondReceived).toEqual([MESSAGES_KIND]);
    });

    test('a throwing listener does not block the others', () => {
        const registry = createChatGroupListenerRegistry();
        const received = [];
        registry.subscribe('g1', () => { throw new Error('boom'); });
        registry.subscribe('g1', (kind) => received.push(kind));
        expect(() => registry.notify('g1', MESSAGES_KIND)).not.toThrow();
        expect(received).toEqual([MESSAGES_KIND]);
    });

    test('notifyAll reaches listeners across every chat group', () => {
        const registry = createChatGroupListenerRegistry();
        const firstReceived = [];
        const secondReceived = [];
        registry.subscribe('g1', (kind) => firstReceived.push(kind));
        registry.subscribe('g2', (kind) => secondReceived.push(kind));
        registry.notifyAll(MESSAGES_KIND);
        expect(firstReceived).toEqual([MESSAGES_KIND]);
        expect(secondReceived).toEqual([MESSAGES_KIND]);
    });

    test('notifyAll swallows throwing listeners and still reaches the rest', () => {
        const registry = createChatGroupListenerRegistry();
        const received = [];
        registry.subscribe('g1', () => { throw new Error('boom'); });
        registry.subscribe('g2', (kind) => received.push(kind));
        expect(() => registry.notifyAll(MEMBERSHIP_KIND)).not.toThrow();
        expect(received).toEqual([MEMBERSHIP_KIND]);
    });

    test('subscribing without a chat group id returns a safe no op unsubscribe', () => {
        const registry = createChatGroupListenerRegistry();
        const unsubscribe = registry.subscribe(null, () => {});
        expect(typeof unsubscribe).toBe('function');
        expect(() => unsubscribe()).not.toThrow();
    });

    test('subscribing without a function listener returns a safe no op unsubscribe', () => {
        const registry = createChatGroupListenerRegistry();
        const unsubscribe = registry.subscribe('g1', null);
        expect(() => registry.notify('g1', MESSAGES_KIND)).not.toThrow();
        expect(() => unsubscribe()).not.toThrow();
    });

    test('resubscribing after a full unsubscribe works again', () => {
        const registry = createChatGroupListenerRegistry();
        const received = [];
        const unsubscribe = registry.subscribe('g1', (kind) => received.push(kind));
        unsubscribe();
        registry.subscribe('g1', (kind) => received.push(kind));
        registry.notify('g1', MESSAGES_KIND);
        expect(received).toEqual([MESSAGES_KIND]);
    });
});

describe('createSubscriptionCounter', () => {
    test('fires onFirstAcquire only when the count goes from zero to one', () => {
        const events = [];
        const counter = createSubscriptionCounter(() => events.push('first'), () => events.push('last'));
        counter.acquire();
        counter.acquire();
        counter.acquire();
        expect(events).toEqual(['first']);
        expect(counter.count()).toBe(3);
    });

    test('fires onLastRelease only when the count returns to zero', () => {
        const events = [];
        const counter = createSubscriptionCounter(() => events.push('first'), () => events.push('last'));
        const releaseFirst = counter.acquire();
        const releaseSecond = counter.acquire();
        releaseFirst();
        expect(events).toEqual(['first']);
        releaseSecond();
        expect(events).toEqual(['first', 'last']);
        expect(counter.count()).toBe(0);
    });

    test('releasing twice is a safe no op', () => {
        const events = [];
        const counter = createSubscriptionCounter(() => events.push('first'), () => events.push('last'));
        const release = counter.acquire();
        release();
        release();
        expect(counter.count()).toBe(0);
        expect(events).toEqual(['first', 'last']);
    });

    test('reacquiring after full release fires onFirstAcquire again', () => {
        const events = [];
        const counter = createSubscriptionCounter(() => events.push('first'), () => events.push('last'));
        const release = counter.acquire();
        release();
        counter.acquire();
        expect(events).toEqual(['first', 'last', 'first']);
    });

    test('throwing callbacks are swallowed and counting stays correct', () => {
        const counter = createSubscriptionCounter(() => { throw new Error('boom'); }, () => { throw new Error('boom'); });
        let release = null;
        expect(() => { release = counter.acquire(); }).not.toThrow();
        expect(counter.count()).toBe(1);
        expect(() => release()).not.toThrow();
        expect(counter.count()).toBe(0);
    });

    test('interleaved acquires and releases keep an accurate count', () => {
        const counter = createSubscriptionCounter(() => {}, () => {});
        const releaseFirst = counter.acquire();
        const releaseSecond = counter.acquire();
        releaseFirst();
        const releaseThird = counter.acquire();
        expect(counter.count()).toBe(2);
        releaseSecond();
        releaseThird();
        expect(counter.count()).toBe(0);
    });
});