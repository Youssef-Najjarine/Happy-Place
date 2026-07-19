import { useEffect, useRef } from 'react';
import { AppState } from 'react-native';
import { useDispatch } from 'react-redux';
import { setUser, clearUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import authenticationService from 'services/authenticationService';

const BASE_RETRY_DELAY_MS = 1500;
const MAX_RETRY_DELAY_MS = 15000;

export default function useUserHydration() {
    const dispatch = useDispatch();
    const hydrationSeqRef = useRef(0);
    const hydratedRef = useRef(false);

    useEffect(() => {
        let retryTimerId = null;
        const clearRetryTimer = () => {
            if (retryTimerId) {
                clearTimeout(retryTimerId);
                retryTimerId = null;
            }
        };
        const scheduleRetry = (seq, authToken, attempt) => {
            clearRetryTimer();
            const delay = Math.min(BASE_RETRY_DELAY_MS * Math.pow(2, attempt - 1), MAX_RETRY_DELAY_MS);
            retryTimerId = setTimeout(() => {
                if (hydrationSeqRef.current === seq && !hydratedRef.current) hydrate(seq, authToken, attempt + 1);
            }, delay);
        };
        const hydrate = (seq, authToken, attempt) => {
            const run = async () => {
                try {
                    const response = await authenticationService.validateToken(authToken);
                    if (hydrationSeqRef.current !== seq) return;
                    if (response.status === 401) {
                        hydratedRef.current = false;
                        await tokenStorage.clearToken();
                        return;
                    }
                    if (!response.ok) {
                        scheduleRetry(seq, authToken, attempt);
                        return;
                    }
                    const profileData = await response.json();
                    if (hydrationSeqRef.current !== seq) return;
                    hydratedRef.current = true;
                    dispatch(setUser(profileData));
                } catch {
                    if (hydrationSeqRef.current !== seq) return;
                    scheduleRetry(seq, authToken, attempt);
                }
            };
            run();
        };
        const unsubscribe = tokenStorage.subscribe((authToken) => {
            const seq = hydrationSeqRef.current + 1;
            hydrationSeqRef.current = seq;
            clearRetryTimer();
            hydratedRef.current = false;
            if (!authToken) {
                dispatch(clearUser());
                return;
            }
            hydrate(seq, authToken, 1);
        });
        const hydrateExistingToken = async () => {
            const existingToken = await tokenStorage.getToken();
            if (!existingToken) return;
            if (hydrationSeqRef.current !== 0) return;
            const seq = hydrationSeqRef.current + 1;
            hydrationSeqRef.current = seq;
            hydrate(seq, existingToken, 1);
        };
        hydrateExistingToken();
        const appStateSubscription = AppState.addEventListener('change', (nextAppState) => {
            if (nextAppState !== 'active') return;
            if (hydratedRef.current) return;
            const kick = async () => {
                const token = await tokenStorage.getToken();
                if (!token || hydratedRef.current) return;
                const seq = hydrationSeqRef.current + 1;
                hydrationSeqRef.current = seq;
                clearRetryTimer();
                hydrate(seq, token, 1);
            };
            kick();
        });
        return () => {
            clearRetryTimer();
            appStateSubscription.remove();
            unsubscribe();
        };
    }, [dispatch]);
}