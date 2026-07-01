import { configureStore } from '@reduxjs/toolkit';
import loadingReducer from './loadingSlice';
import userReducer from './userSlice';
import { helpApi } from './helpApi';

const store = configureStore({
  reducer: {
    loading: loadingReducer,
    user: userReducer,
    [helpApi.reducerPath]: helpApi.reducer,
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(helpApi.middleware),
});

export default store;