import { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigation } from '@react-navigation/native';
import tokenStorage from 'src/services/tokenStorage';
import authenticationService from 'src/services/authenticationService';
import helpSessionStorage from 'src/services/helpSessionStorage';
import { showToast } from 'src/components/Toast';
import { useOpenRequestsQuery, usePollOfferQuery, useSetAvailabilityMutation } from 'src/store/helpApi';

const LISTEN_INTERVAL_MS = 3000;
const READY_INTERVAL_MS = 3000;

export default function useHelperListen() {
    const navigation = useNavigation();
    const [listening, setListening] = useState(false);
    const [authToken, setAuthToken] = useState(null);
    const [setAvailability] = useSetAvailabilityMutation();
    const intentSeqRef = useRef(0);
    const intentRef = useRef({ desired: null, running: false });

    const applyIntent = useCallback((seq, token, wantListening) => {
        const current = intentRef.current.desired;
        if (current && current.seq > seq) return;
        intentRef.current.desired = { seq, token, wantListening };
        if (intentRef.current.running) return;
        intentRef.current.running = true;
        const run = async () => {
            while (true) {
                const next = intentRef.current.desired;
                try {
                    if (next.wantListening) {
                        await helpSessionStorage.saveListening();
                        if (next.token) {
                            await setAvailability({ authToken: next.token, isAvailable: true }).unwrap();
                        }
                    } else {
                        await helpSessionStorage.clear();
                        if (next.token) {
                            await setAvailability({ authToken: next.token, isAvailable: false }).unwrap();
                        }
                    }
                } catch (error) {
                }
                if (intentRef.current.desired === next) {
                    intentRef.current.running = false;
                    return;
                }
            }
        };
        run();
    }, [setAvailability]);

    useEffect(() => {
        let cancelled = false;
        const load = async () => {
            const session = await helpSessionStorage.get();
            if (cancelled || !session || session.mode !== 'listening') return;
            const token = await tokenStorage.getToken();
            if (cancelled || !token) return;
            let validation = null;
            try {
                validation = await authenticationService.validateToken(token);
            } catch (error) {
                return;
            }
            if (cancelled || !validation.ok) return;
            const seq = intentSeqRef.current + 1;
            intentSeqRef.current = seq;
            setAuthToken(token);
            setListening(true);
            applyIntent(seq, token, true);
        };
        load();
        return () => {
            cancelled = true;
        };
    }, [applyIntent]);

    const { data: openRequestsData, error: openRequestsError } = useOpenRequestsQuery(
        authToken,
        { skip: !authToken || !listening, pollingInterval: LISTEN_INTERVAL_MS, refetchOnMountOrArgChange: true }
    );

    const { data: startedData } = usePollOfferQuery(
        authToken,
        { skip: !authToken, pollingInterval: READY_INTERVAL_MS, refetchOnMountOrArgChange: true }
    );

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

    const handleAuthFailure = useCallback(async () => {
        const seq = intentSeqRef.current + 1;
        intentSeqRef.current = seq;
        await tokenStorage.clearToken();
        setAuthToken(null);
        setListening(false);
        applyIntent(seq, null, false);
        navigation.navigate('LoginOptions');
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
            handleAuthFailure();
        }
    }, [listening, openRequestsError, handleAuthFailure]);

    const pendingCount = listening && Array.isArray(openRequestsData) ? openRequestsData.length : 0;
    const readyCount = authToken && Array.isArray(startedData) ? startedData.length : 0;

    return { listening, pendingCount, readyCount, offeredCount, startListening, stopListening, openPicker };
}