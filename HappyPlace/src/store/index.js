import { configureStore } from '@reduxjs/toolkit';
import loadingReducer from './loadingSlice';
import userReducer from './userSlice';
import realtimeReducer from './realtimeSlice';
import { helpApi } from './helpApi';
import { chatGroupsApi } from './chatGroupsApi';
import { friendsApi } from './friendsApi';

const store = configureStore({
  reducer: {
    loading: loadingReducer,
    user: userReducer,
    realtime: realtimeReducer,
    [helpApi.reducerPath]: helpApi.reducer,
    [chatGroupsApi.reducerPath]: chatGroupsApi.reducer,
    [friendsApi.reducerPath]: friendsApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(helpApi.middleware).concat(chatGroupsApi.middleware).concat(friendsApi.middleware),
});

export default store;