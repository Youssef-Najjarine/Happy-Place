const HEALTHY_SCREEN_POLL_MS = 30000;
const HEALTHY_BADGE_POLL_MS = 60000;
const FAST_MESSAGES_POLL_MS = 2000;
const FAST_LIST_POLL_MS = 5000;
const FAST_MEMBERS_POLL_MS = 3000;
const FAST_HELP_POLL_MS = 3000;
const FAST_BADGE_POLL_MS = 15000;

export function messagesPollingInterval(isRealtimeConnected) {
    return isRealtimeConnected ? HEALTHY_SCREEN_POLL_MS : FAST_MESSAGES_POLL_MS;
}

export function listPollingInterval(isRealtimeConnected, isFocused) {
    if (!isFocused) {
        return 0;
    }
    return isRealtimeConnected ? HEALTHY_SCREEN_POLL_MS : FAST_LIST_POLL_MS;
}

export function membersPollingInterval(isRealtimeConnected, isFocused) {
    if (!isFocused) {
        return 0;
    }
    return isRealtimeConnected ? HEALTHY_SCREEN_POLL_MS : FAST_MEMBERS_POLL_MS;
}

export function helpPollingInterval(isRealtimeConnected) {
    return isRealtimeConnected ? HEALTHY_SCREEN_POLL_MS : FAST_HELP_POLL_MS;
}

export function unreadBadgePollingInterval(isRealtimeConnected) {
    return isRealtimeConnected ? HEALTHY_BADGE_POLL_MS : FAST_BADGE_POLL_MS;
}