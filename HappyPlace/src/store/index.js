import { configureStore } from '@reduxjs/toolkit';
import loadingReducer from './loadingSlice';
import userReducer from './userSlice';

const store = configureStore({
  reducer: {
    loading: loadingReducer,
    user: userReducer,
  },
});

export default store;