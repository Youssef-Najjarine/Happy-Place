import { createSlice } from '@reduxjs/toolkit';

const userSlice = createSlice({
  name: 'user',
  initialState: {
    displayName: null,
    username: null,
    avatarColor: null,
    profilePhotoUrl: null,
    isLoggedIn: false,
  },
  reducers: {
    setUser: (state, action) => {
      state.displayName = action.payload.displayName;
      state.username = action.payload.username;
      state.avatarColor = action.payload.avatarColor;
      state.profilePhotoUrl = action.payload.profilePhotoUrl;
      state.isLoggedIn = true;
    },
    clearUser: (state) => {
      state.displayName = null;
      state.username = null;
      state.avatarColor = null;
      state.profilePhotoUrl = null;
      state.isLoggedIn = false;
    },
  },
});

export const { setUser, clearUser } = userSlice.actions;
export default userSlice.reducer;