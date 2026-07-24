import { createSlice } from '@reduxjs/toolkit';

const realtimeSlice = createSlice({
    name: 'realtime',
    initialState: {
        isConnected: false,
        helpChangedTick: 0,
    },
    reducers: {
        setRealtimeConnected: (state, action) => {
            state.isConnected = action.payload === true;
        },
        bumpHelpChanged: (state) => {
            state.helpChangedTick += 1;
        },
    },
});

export const { setRealtimeConnected, bumpHelpChanged } = realtimeSlice.actions;
export const selectRealtimeConnected = (state) => state.realtime.isConnected;
export const selectHelpChangedTick = (state) => state.realtime.helpChangedTick;
export default realtimeSlice.reducer;