import { useEffect } from 'react';
import { useDispatch } from 'react-redux';
import realtimeService from 'src/services/realtimeService';

export default function useRealtime() {
    const dispatch = useDispatch();
    useEffect(() => {
        realtimeService.initialize(dispatch);
        return () => {
            realtimeService.teardown();
        };
    }, [dispatch]);
}