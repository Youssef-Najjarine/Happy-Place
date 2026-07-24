import { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigation, useIsFocused } from '@react-navigation/native';
import { useSelector } from 'react-redux';
import tokenStorage from 'src/services/tokenStorage';
import authenticationService from 'src/services/authenticationService';
import helpSessionStorage from 'src/services/helpSessionStorage';
import realtimeService from 'src/services/realtimeService';
import { selectRealtimeConnected, selectHelpChangedTick } from 'src/store/realtimeSlice';
import { helpPollingInterval } from 'src/utils/pollingPolicy';
import { showToast } from 'src/components/Toast';
import { useOpenRequestsQuery, usePollOfferQuery, useSetAvailabilityMutation, useLazyGetAvailabilityQuery } from 'src/store/helpApi';

const INTENT_RETRY_BASE_MS = 1500;
const INTENT_RETRY_MAX_MS = 15000;

export default function useHelperListen() {
    const navigation = useNavigation();
    const isFocused = useIsFocused();
    const [listening, setListening] = useState(false);
    const [authToken, setAuthToken] = useState(null);
    const [setAvailability] = useSetAvailabilityMutation();
    const [triggerGetAvailability] = useLazyGetAvailabilityQuery();
    const intentSeqRef = useRef(0);
    const intentRef = useRef({ desired: null, running: false });
    const aliveRef = useRef(true);

    const isRealtimeConnected = useSelector(selectRealtimeConnected);
    const helpChangedTick = useSelector(selectHelpChangedTick);
    const previousHelpTickRef = useRef(helpChangedTick);

    useEffect(() => {
        aliveRef.current = true;
        return () => {
            aliveRef.current = false;
        };
    }, []);

    const waitBeforeRetry = useCallback((delayMs) => {
        return new Promise((resolve) => setTimeout(resolve, delayMs));
    }, []);

    const applyIntent = useCallback((seq, token, wantListening) => {
        const currentDesired = intentRef.current.desired;
        if (currentDesired && currentDesired.seq > seq) return;
        intentRef.current.desired = { seq, token, wantListening };
        if (intentRef.current.running) return;
        intentRef.current.running = true;
        const run = async () => {
            let retryDelayMs = INTENT_RETRY_BASE_MS;
            while (aliveRef.current) {
                const next = intentRef.current.desired;
                let applied = false;
                try {
                    if (next.wantListening) {
                        if (next.token) {
                            await helpSessionStorage.saveListening(next.token);
                            const result = await setAvailability({ authToken: next.token, isAvailable: true }).unwrap();
                            if (result && result.status === 'seeking') {
                                await helpSessionStorage.clear();
                                if (intentRef.current.desired === next) {
                                    setListening(false);
                                    showToast('Cancel your Help Me request first', 'info');
                                }
                            }
                        }
                        applied = true;
                    } else {
                        await helpSessionStorage.clear();
                        if (next.token) {
                            await setAvailability({ authToken: next.token, isAvailable: false }).unwrap();
                        }
                        applied = true;
                    }
                } catch (error) {
                    if (error && error.status === 401) {
                        await helpSessionStorage.clear();
                        if (intentRef.current.desired === next) {
                            setListening(false);
                        }
                        applied = true;
                    }
                }
                if (applied) {
                    if (intentRef.current.desired === next) {
                        intentRef.current.running = false;
                        return;
                    }
                    retryDelayMs = INTENT_RETRY_BASE_MS;
                    continue;
                }
                if (intentRef.current.desired !== next) {
                    retryDelayMs = INTENT_RETRY_BASE_MS;
                    continue;
                }
                await waitBeforeRetry(retryDelayMs);
                retryDelayMs = Math.min(retryDelayMs * 2, INTENT_RETRY_MAX_MS);
            }
            intentRef.current.running = false;
        };
        run();
    }, [setAvailability, waitBeforeRetry]);

    useEffect(() => {
        let cancelled = false;
        const beginListening = (token) => {
            const seq = intentSeqRef.current + 1;
            intentSeqRef.current = seq;
            setAuthToken(token);
            setListening(true);
            applyIntent(seq, token, true);
        };
        const load = async () => {
            const token = await tokenStorage.getToken();
            if (cancelled || !token) return;
            const session = await helpSessionStorage.get();
            if (cancelled || intentSeqRef.current !== 0) return;
            const hasOwnListeningSession = !!session && session.mode === 'listening' && session.ownerToken === token;
            if (hasOwnListeningSession) {
                let validation = null;
                try {
                    validation = await authenticationService.validateToken(token);
                } catch (error) {
                    return;
                }
                if (cancelled || !validation.ok || intentSeqRef.current !== 0) return;
                beginListening(token);
                return;
            }
            if (session && session.mode === 'seeking' && session.ownerToken === token) return;
            let availabilityResult = null;
            try {
                availabilityResult = await triggerGetAvailability(token).unwrap();
            } catch (error) {
                return;
            }
            if (cancelled || intentSeqRef.current !== 0) return;
            if (!availabilityResult || availabilityResult.status !== 'ok' || !availabilityResult.isAvailable) return;
            beginListening(token);
        };
        load();
        return () => {
            cancelled = true;
        };
    }, [applyIntent, triggerGetAvailability]);

    const { data: openRequestsData, error: openRequestsError, refetch: refetchOpenRequests } = useOpenRequestsQuery(
        authToken,
        { skip: !authToken || !listening, pollingInterval: helpPollingInterval(isRealtimeConnected), refetchOnMountOrArgChange: true }
    );

    const { data: startedData, refetch: refetchStartedGroups } = usePollOfferQuery(
        authToken,
        { skip: !authToken, pollingInterval: helpPollingInterval(isRealtimeConnected), refetchOnMountOrArgChange: true }
    );

    useEffect(() => {
        if (helpChangedTick === previousHelpTickRef.current) return;
        previousHelpTickRef.current = helpChangedTick;
        if (!authToken) return;
        if (listening) refetchOpenRequests();
        refetchStartedGroups();
    }, [helpChangedTick, authToken, listening, refetchOpenRequests, refetchStartedGroups]);

    useEffect(() => {
        if (!listening) return undefined;
        return realtimeService.acquireOpenRequestsSubscription();
    }, [listening]);

    const offeredCount = listening && Array.isArray(openRequestsData)
        ? openRequestsData.filter((request) => request.offerStatus === 'offered').length
        : 0;

    const resolveToken = useCallback(async () => {
        const existing = await tokenStorage.getToken();
        if (existing) {
            const validation = await authenticationService.validateToken(existing);
            if (validation.status !== 401) return existing;
            await tokenStorage.clearToken();
        }
        return tokenStorage.ensureGuestToken();
    }, []);

    const handleAuthFailure = useCallback(async (shouldNavigate) => {
        const seq = intentSeqRef.current + 1;
        intentSeqRef.current = seq;
        await tokenStorage.clearToken();
        setAuthToken(null);
        setListening(false);
        applyIntent(seq, null, false);
        if (shouldNavigate) {
            navigation.navigate('LoginOptions');
        }
    }, [navigation, applyIntent]);

    const startListening = useCallback(async () => {
        const seq = intentSeqRef.current + 1;
        intentSeqRef.current = seq;
        setListening(true);
        let token = null;
        try {
            token = await resolveToken();
        } catch (error) {
            token = null;
        }
        if (intentSeqRef.current !== seq) return;
        if (!token) {
            setListening(false);
            applyIntent(seq, null, false);
            showToast('Couldn\u2019t reach the server \u2014 tap to try again', 'info');
            return;
        }
        setAuthToken(token);
        applyIntent(seq, token, true);
    }, [resolveToken, applyIntent]);

    const stopListening = useCallback(async () => {
        const seq = intentSeqRef.current + 1;
        intentSeqRef.current = seq;
        setListening(false);
        const token = await tokenStorage.getToken();
        if (intentSeqRef.current !== seq) return;
        applyIntent(seq, token, false);
    }, [applyIntent]);

    const openPicker = useCallback((targetHelpView) => {
        navigation.setParams({ helpView: targetHelpView });
    }, [navigation]);

    useEffect(() => {
        if (!listening || !openRequestsError) return;
        if (openRequestsError.status === 401) {
            handleAuthFailure(isFocused);
        }
    }, [listening, openRequestsError, handleAuthFailure, isFocused]);

    const pendingCount = listening && Array.isArray(openRequestsData) ? openRequestsData.length : 0;
    const readyCount = authToken && Array.isArray(startedData) ? startedData.length : 0;

    return { listening, pendingCount, readyCount, offeredCount, startListening, stopListening, openPicker };
}