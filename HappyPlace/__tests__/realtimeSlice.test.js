import realtimeReducer, {
    setRealtimeConnected,
    bumpHelpChanged,
    selectRealtimeConnected,
    selectHelpChangedTick,
} from '../src/store/realtimeSlice';

describe('realtimeSlice reducer', () => {
    test('starts disconnected with a zero help tick', () => {
        const state = realtimeReducer(undefined, { type: '@@INIT' });
        expect(state).toEqual({ isConnected: false, helpChangedTick: 0 });
    });

    test('setRealtimeConnected true marks the socket connected', () => {
        const state = realtimeReducer(undefined, setRealtimeConnected(true));
        expect(state.isConnected).toBe(true);
    });

    test('setRealtimeConnected false marks the socket disconnected', () => {
        let state = realtimeReducer(undefined, setRealtimeConnected(true));
        state = realtimeReducer(state, setRealtimeConnected(false));
        expect(state.isConnected).toBe(false);
    });

    test('only a strict boolean true counts as connected', () => {
        expect(realtimeReducer(undefined, setRealtimeConnected(1)).isConnected).toBe(false);
        expect(realtimeReducer(undefined, setRealtimeConnected('true')).isConnected).toBe(false);
        expect(realtimeReducer(undefined, setRealtimeConnected(null)).isConnected).toBe(false);
        expect(realtimeReducer(undefined, setRealtimeConnected(undefined)).isConnected).toBe(false);
    });

    test('bumpHelpChanged increments the tick monotonically', () => {
        let state = realtimeReducer(undefined, { type: '@@INIT' });
        state = realtimeReducer(state, bumpHelpChanged());
        expect(state.helpChangedTick).toBe(1);
        state = realtimeReducer(state, bumpHelpChanged());
        expect(state.helpChangedTick).toBe(2);
        state = realtimeReducer(state, bumpHelpChanged());
        expect(state.helpChangedTick).toBe(3);
    });

    test('bumping the help tick does not touch the connected flag', () => {
        let state = realtimeReducer(undefined, setRealtimeConnected(true));
        state = realtimeReducer(state, bumpHelpChanged());
        expect(state.isConnected).toBe(true);
    });

    test('unknown actions leave the state values unchanged', () => {
        let state = realtimeReducer(undefined, setRealtimeConnected(true));
        state = realtimeReducer(state, bumpHelpChanged());
        const nextState = realtimeReducer(state, { type: 'something/else' });
        expect(nextState).toEqual({ isConnected: true, helpChangedTick: 1 });
    });
});

describe('realtimeSlice selectors', () => {
    test('selectRealtimeConnected reads the connected flag', () => {
        expect(selectRealtimeConnected({ realtime: { isConnected: true, helpChangedTick: 0 } })).toBe(true);
        expect(selectRealtimeConnected({ realtime: { isConnected: false, helpChangedTick: 0 } })).toBe(false);
    });

    test('selectHelpChangedTick reads the tick', () => {
        expect(selectHelpChangedTick({ realtime: { isConnected: false, helpChangedTick: 7 } })).toBe(7);
    });
});