const OverrideDisagreementTolerance = 2;

export function createOverlayState() {
    return { overrides: {}, hiddenRequests: {}, hiddenStarted: {} };
}

export function applyOverride(overlayState, chatGroupId, offerStatus) {
    if (!chatGroupId) return overlayState;
    return {
        ...overlayState,
        overrides: { ...overlayState.overrides, [chatGroupId]: { offerStatus, settled: false, disagreementCount: 0 } }
    };
}

export function settleOverride(overlayState, chatGroupId) {
    const existingOverride = overlayState.overrides[chatGroupId];
    if (!existingOverride || existingOverride.settled) return overlayState;
    return {
        ...overlayState,
        overrides: { ...overlayState.overrides, [chatGroupId]: { ...existingOverride, settled: true, disagreementCount: 0 } }
    };
}

export function removeOverride(overlayState, chatGroupId) {
    if (!(chatGroupId in overlayState.overrides)) return overlayState;
    const remainingOverrides = { ...overlayState.overrides };
    delete remainingOverrides[chatGroupId];
    return { ...overlayState, overrides: remainingOverrides };
}

export function hideRequest(overlayState, chatGroupId) {
    if (!chatGroupId || overlayState.hiddenRequests[chatGroupId]) return overlayState;
    return { ...overlayState, hiddenRequests: { ...overlayState.hiddenRequests, [chatGroupId]: true } };
}

export function unhideRequest(overlayState, chatGroupId) {
    if (!overlayState.hiddenRequests[chatGroupId]) return overlayState;
    const remainingHidden = { ...overlayState.hiddenRequests };
    delete remainingHidden[chatGroupId];
    return { ...overlayState, hiddenRequests: remainingHidden };
}

export function hideStartedGroup(overlayState, chatGroupId) {
    if (!chatGroupId || overlayState.hiddenStarted[chatGroupId]) return overlayState;
    return { ...overlayState, hiddenStarted: { ...overlayState.hiddenStarted, [chatGroupId]: true } };
}

export function unhideStartedGroup(overlayState, chatGroupId) {
    if (!overlayState.hiddenStarted[chatGroupId]) return overlayState;
    const remainingHidden = { ...overlayState.hiddenStarted };
    delete remainingHidden[chatGroupId];
    return { ...overlayState, hiddenStarted: remainingHidden };
}

export function reconcileWithServer(overlayState, serverRequests) {
    if (!Array.isArray(serverRequests)) return overlayState;
    const overrideChatGroupIds = Object.keys(overlayState.overrides);
    if (overrideChatGroupIds.length === 0) return overlayState;
    const serverStatusByChatGroupId = {};
    serverRequests.forEach((serverRequest) => { serverStatusByChatGroupId[serverRequest.chatGroupId] = serverRequest.offerStatus; });
    let nextOverrides = null;
    const ensureNextOverrides = () => {
        if (nextOverrides == null) nextOverrides = { ...overlayState.overrides };
        return nextOverrides;
    };
    overrideChatGroupIds.forEach((chatGroupId) => {
        const override = overlayState.overrides[chatGroupId];
        const serverStatus = serverStatusByChatGroupId[chatGroupId];
        if (serverStatus === undefined || serverStatus === override.offerStatus) {
            delete ensureNextOverrides()[chatGroupId];
            return;
        }
        if (!override.settled) return;
        const disagreementCount = override.disagreementCount + 1;
        if (disagreementCount >= OverrideDisagreementTolerance) {
            delete ensureNextOverrides()[chatGroupId];
            return;
        }
        ensureNextOverrides()[chatGroupId] = { ...override, disagreementCount };
    });
    if (nextOverrides == null) return overlayState;
    return { ...overlayState, overrides: nextOverrides };
}

export function projectRequests(overlayState, serverRequests) {
    const requests = Array.isArray(serverRequests) ? serverRequests : [];
    return requests
        .filter((serverRequest) => !overlayState.hiddenRequests[serverRequest.chatGroupId])
        .map((serverRequest) => {
            const override = overlayState.overrides[serverRequest.chatGroupId];
            if (!override) return serverRequest;
            return { ...serverRequest, offerStatus: override.offerStatus };
        });
}

export function projectStartedGroups(overlayState, serverStartedGroups) {
    const startedGroups = Array.isArray(serverStartedGroups) ? serverStartedGroups : [];
    return startedGroups.filter((startedGroup) => !overlayState.hiddenStarted[startedGroup.chatGroupId]);
}