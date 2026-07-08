import { configureStore } from '@reduxjs/toolkit';
import loadingReducer from './loadingSlice';
import userReducer from './userSlice';
import { helpApi } from './helpApi';
import { chatGroupsApi } from './chatGroupsApi';

const store = configureStore({
  reducer: {
    loading: loadingReducer,
    user: userReducer,
    [helpApi.reducerPath]: helpApi.reducer,
    [chatGroupsApi.reducerPath]: chatGroupsApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(helpApi.middleware).concat(chatGroupsApi.middleware),
});

export default store;