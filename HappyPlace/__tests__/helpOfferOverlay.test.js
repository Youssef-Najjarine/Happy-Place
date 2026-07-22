import {
    createOverlayState,
    applyOverride,
    settleOverride,
    removeOverride,
    hideRequest,
    unhideRequest,
    hideStartedGroup,
    unhideStartedGroup,
    reconcileWithServer,
    projectRequests,
    projectStartedGroups
} from '../src/utils/helpOfferOverlay';

const request = (chatGroupId, offerStatus) => ({ chatGroupId, chatGroupName: `Group ${chatGroupId}`, createdAtUtc: '2026-07-21T00:00:00Z', offerStatus });
const startedGroup = (chatGroupId) => ({ chatGroupId, chatGroupName: `Group ${chatGroupId}` });

describe('createOverlayState', () => {
    test('starts with no overrides and nothing hidden', () => {
        const overlayState = createOverlayState();
        expect(overlayState.overrides).toEqual({});
        expect(overlayState.hiddenRequests).toEqual({});
        expect(overlayState.hiddenStarted).toEqual({});
    });
});

describe('applyOverride', () => {
    test('records an unsettled override with the given status', () => {
        const overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        expect(overlayState.overrides.g1).toEqual({ offerStatus: 'offered', settled: false, disagreementCount: 0 });
    });

    test('replaces an existing override and resets its lifecycle', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        overlayState = applyOverride(overlayState, 'g1', 'none');
        expect(overlayState.overrides.g1).toEqual({ offerStatus: 'none', settled: false, disagreementCount: 0 });
    });

    test('returns the same state for a missing chatGroupId', () => {
        const overlayState = createOverlayState();
        expect(applyOverride(overlayState, null, 'offered')).toBe(overlayState);
        expect(applyOverride(overlayState, undefined, 'offered')).toBe(overlayState);
    });

    test('does not mutate the previous state', () => {
        const overlayState = createOverlayState();
        applyOverride(overlayState, 'g1', 'offered');
        expect(overlayState.overrides).toEqual({});
    });
});

describe('settleOverride', () => {
    test('marks an override settled and clears its disagreement count', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        overlayState = settleOverride(overlayState, 'g1');
        expect(overlayState.overrides.g1).toEqual({ offerStatus: 'offered', settled: true, disagreementCount: 0 });
    });

    test('returns the same state when the override is missing', () => {
        const overlayState = createOverlayState();
        expect(settleOverride(overlayState, 'g1')).toBe(overlayState);
    });

    test('returns the same state when already settled', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        expect(settleOverride(overlayState, 'g1')).toBe(overlayState);
    });
});

describe('removeOverride', () => {
    test('removes only the targeted override', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = applyOverride(overlayState, 'g2', 'none');
        overlayState = removeOverride(overlayState, 'g1');
        expect(overlayState.overrides.g1).toBeUndefined();
        expect(overlayState.overrides.g2).toBeDefined();
    });

    test('returns the same state when the override is missing', () => {
        const overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        expect(removeOverride(overlayState, 'g2')).toBe(overlayState);
    });
});

describe('hiding requests and started groups', () => {
    test('hideRequest and unhideRequest round trip', () => {
        let overlayState = hideRequest(createOverlayState(), 'g1');
        expect(overlayState.hiddenRequests.g1).toBe(true);
        overlayState = unhideRequest(overlayState, 'g1');
        expect(overlayState.hiddenRequests.g1).toBeUndefined();
    });

    test('hideStartedGroup and unhideStartedGroup round trip', () => {
        let overlayState = hideStartedGroup(createOverlayState(), 'g1');
        expect(overlayState.hiddenStarted.g1).toBe(true);
        overlayState = unhideStartedGroup(overlayState, 'g1');
        expect(overlayState.hiddenStarted.g1).toBeUndefined();
    });

    test('hiding twice and unhiding a missing id return the same state', () => {
        const hiddenState = hideRequest(createOverlayState(), 'g1');
        expect(hideRequest(hiddenState, 'g1')).toBe(hiddenState);
        expect(unhideRequest(hiddenState, 'g2')).toBe(hiddenState);
        const hiddenStartedState = hideStartedGroup(createOverlayState(), 'g1');
        expect(hideStartedGroup(hiddenStartedState, 'g1')).toBe(hiddenStartedState);
        expect(unhideStartedGroup(hiddenStartedState, 'g2')).toBe(hiddenStartedState);
    });

    test('hiding with a missing chatGroupId returns the same state', () => {
        const overlayState = createOverlayState();
        expect(hideRequest(overlayState, null)).toBe(overlayState);
        expect(hideStartedGroup(overlayState, undefined)).toBe(overlayState);
    });
});

describe('reconcileWithServer', () => {
    test('returns the same state for a non array snapshot', () => {
        const overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        expect(reconcileWithServer(overlayState, undefined)).toBe(overlayState);
        expect(reconcileWithServer(overlayState, null)).toBe(overlayState);
    });

    test('returns the same state when there are no overrides', () => {
        const overlayState = createOverlayState();
        expect(reconcileWithServer(overlayState, [request('g1', 'none')])).toBe(overlayState);
    });

    test('drops an override once the server agrees with it', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'offered')]);
        expect(overlayState.overrides.g1).toBeUndefined();
    });

    test('drops an override when its request disappears from the snapshot', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = reconcileWithServer(overlayState, [request('g2', 'none')]);
        expect(overlayState.overrides.g1).toBeUndefined();
    });

    test('drops an unsettled override when its request disappears', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = reconcileWithServer(overlayState, []);
        expect(overlayState.overrides.g1).toBeUndefined();
    });

    test('keeps an unsettled override across any number of disagreeing snapshots', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        expect(overlayState.overrides.g1).toEqual({ offerStatus: 'offered', settled: false, disagreementCount: 0 });
    });

    test('a disagreeing snapshot against only unsettled overrides returns the same state', () => {
        const overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        expect(reconcileWithServer(overlayState, [request('g1', 'none')])).toBe(overlayState);
    });

    test('tolerates one disagreeing snapshot after settling then yields to the server', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        expect(overlayState.overrides.g1).toEqual({ offerStatus: 'offered', settled: true, disagreementCount: 1 });
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        expect(overlayState.overrides.g1).toBeUndefined();
    });

    test('an agreeing snapshot resets nothing because the override is already gone', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'offered')]);
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        expect(overlayState.overrides.g1).toBeUndefined();
    });

    test('handles agreement disagreement and disappearance together in one snapshot', () => {
        let overlayState = applyOverride(createOverlayState(), 'agrees', 'offered');
        overlayState = applyOverride(overlayState, 'disagrees', 'offered');
        overlayState = applyOverride(overlayState, 'vanished', 'offered');
        overlayState = settleOverride(overlayState, 'agrees');
        overlayState = settleOverride(overlayState, 'disagrees');
        overlayState = settleOverride(overlayState, 'vanished');
        overlayState = reconcileWithServer(overlayState, [request('agrees', 'offered'), request('disagrees', 'none')]);
        expect(overlayState.overrides.agrees).toBeUndefined();
        expect(overlayState.overrides.vanished).toBeUndefined();
        expect(overlayState.overrides.disagrees).toEqual({ offerStatus: 'offered', settled: true, disagreementCount: 1 });
    });

    test('leaves hidden maps untouched', () => {
        let overlayState = hideRequest(createOverlayState(), 'h1');
        overlayState = hideStartedGroup(overlayState, 's1');
        overlayState = applyOverride(overlayState, 'g1', 'offered');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'offered')]);
        expect(overlayState.hiddenRequests.h1).toBe(true);
        expect(overlayState.hiddenStarted.s1).toBe(true);
    });

    test('does not mutate the previous state', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        reconcileWithServer(overlayState, [request('g1', 'none')]);
        expect(overlayState.overrides.g1.disagreementCount).toBe(0);
    });
});

describe('projectRequests', () => {
    test('returns an empty list for a non array snapshot', () => {
        expect(projectRequests(createOverlayState(), undefined)).toEqual([]);
        expect(projectRequests(createOverlayState(), null)).toEqual([]);
    });

    test('passes untouched requests through by reference', () => {
        const serverRequest = request('g1', 'none');
        const projected = projectRequests(createOverlayState(), [serverRequest]);
        expect(projected[0]).toBe(serverRequest);
    });

    test('applies the override status without touching other properties', () => {
        const overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        const projected = projectRequests(overlayState, [request('g1', 'none')]);
        expect(projected[0].offerStatus).toBe('offered');
        expect(projected[0].chatGroupName).toBe('Group g1');
    });

    test('filters hidden requests out', () => {
        const overlayState = hideRequest(createOverlayState(), 'g1');
        const projected = projectRequests(overlayState, [request('g1', 'none'), request('g2', 'none')]);
        expect(projected.map((item) => item.chatGroupId)).toEqual(['g2']);
    });
});

describe('projectStartedGroups', () => {
    test('returns an empty list for a non array snapshot', () => {
        expect(projectStartedGroups(createOverlayState(), undefined)).toEqual([]);
    });

    test('filters hidden started groups out', () => {
        const overlayState = hideStartedGroup(createOverlayState(), 'g1');
        const projected = projectStartedGroups(overlayState, [startedGroup('g1'), startedGroup('g2')]);
        expect(projected.map((item) => item.chatGroupId)).toEqual(['g2']);
    });
});

describe('the stop helping scenario that produced the stale Offered row', () => {
    test('a withdrawn offer stops being shown once the server has spoken twice', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        const withdrawnSnapshot = [request('g1', 'none')];
        overlayState = reconcileWithServer(overlayState, withdrawnSnapshot);
        overlayState = reconcileWithServer(overlayState, withdrawnSnapshot);
        const projected = projectRequests(overlayState, withdrawnSnapshot);
        expect(projected[0].offerStatus).toBe('none');
        expect(projected.filter((item) => item.offerStatus === 'offered')).toEqual([]);
    });

    test('ending the helper session clears every override immediately', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        overlayState = createOverlayState();
        const projected = projectRequests(overlayState, [request('g1', 'none')]);
        expect(projected[0].offerStatus).toBe('none');
    });
});

describe('the stale in flight snapshot race', () => {
    test('one pre mutation snapshot arriving after settlement does not flicker the row back', () => {
        let overlayState = applyOverride(createOverlayState(), 'g1', 'offered');
        overlayState = settleOverride(overlayState, 'g1');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'none')]);
        expect(projectRequests(overlayState, [request('g1', 'none')])[0].offerStatus).toBe('offered');
        overlayState = reconcileWithServer(overlayState, [request('g1', 'offered')]);
        expect(overlayState.overrides.g1).toBeUndefined();
        expect(projectRequests(overlayState, [request('g1', 'offered')])[0].offerStatus).toBe('offered');
    });
});