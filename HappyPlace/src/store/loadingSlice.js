import { createSlice } from '@reduxjs/toolkit';

const loadingSlice = createSlice({
  name: 'loading',
  initialState: {
    activeRequestCount: 0,
    isLoading: false,
  },
  reducers: {
    showLoading: (state) => {
      state.activeRequestCount += 1;
      state.isLoading = state.activeRequestCount > 0;
    },
    hideLoading: (state) => {
      state.activeRequestCount = Math.max(0, state.activeRequestCount - 1);
      state.isLoading = state.activeRequestCount > 0;
    },
  },
});

export const { showLoading, hideLoading } = loadingSlice.actions;
export default loadingSlice.reducer;