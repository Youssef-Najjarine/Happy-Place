import { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigation, useIsFocused } from '@react-navigation/native';
import { useSelector } from 'react-redux';
import tokenStorage from 'src/services/tokenStorage';
import realtimeService from 'src/services/realtimeService';
import { selectRealtimeConnected, selectHelpChangedTick } from 'src/store/realtimeSlice';
import { helpPollingInterval } from 'src/utils/pollingPolicy';
import { showToast } from 'src/components/Toast';
import { createOverlayState, applyOverride, settleOverride, removeOverride, hideRequest, unhideRequest, hideStartedGroup, unhideStartedGroup, reconcileWithServer, projectRequests, projectStartedGroups } from 'src/utils/helpOfferOverlay';
import { useOpenRequestsQuery, usePollOfferQuery, useCreateOfferMutation, useWithdrawOfferMutation, useDeclineOfferMutation, useJoinMutation, useDeclineInviteMutation } from 'src/store/helpApi';

export default function useHelperOffer(helperListening) {
    const navigation = useNavigation();
    const isFocused = useIsFocused();
    const [phase, setPhase] = useState('loading');
    const [authToken, setAuthToken] = useState(null);
    const [overlay, setOverlay] = useState(createOverlayState);
    const previousListeningRef = useRef(!!helperListening);
    const [createOffer] = useCreateOfferMutation();
    const [withdrawOffer] = useWithdrawOfferMutation();
    const [declineOffer] = useDeclineOfferMutation();
    const [joinOffer] = useJoinMutation();
    const [declineInviteRequest] = useDeclineInviteMutation();

    const isRealtimeConnected = useSelector(selectRealtimeConnected);
    const helpChangedTick = useSelector(selectHelpChangedTick);
    const previousHelpTickRef = useRef(helpChangedTick);

    const { data: openRequestsData, error: openRequestsError, fulfilledTimeStamp: openRequestsFulfilledTime, refetch: refetchOpenRequests } = useOpenRequestsQuery(
        authToken,
        { skip: !authToken || phase === 'idle' || !isFocused, pollingInterval: helpPollingInterval(isRealtimeConnected), refetchOnMountOrArgChange: true }
    );

    const { data: startedData, refetch: refetchStartedGroups } = usePollOfferQuery(
        authToken,
        { skip: !authToken || phase === 'idle' || !isFocused, pollingInterval: helpPollingInterval(isRealtimeConnected) }
    );

    useEffect(() => {
        if (helpChangedTick === previousHelpTickRef.current) return;
        previousHelpTickRef.current = helpChangedTick;
        if (!authToken || phase === 'idle' || !isFocused) return;
        refetchOpenRequests();
        refetchStartedGroups();
    }, [helpChangedTick, authToken, phase, isFocused, refetchOpenRequests, refetchStartedGroups]);

    useEffect(() => {
        if (!authToken || phase === 'idle' || !isFocused) return undefined;
        return realtimeService.acquireOpenRequestsSubscription();
    }, [authToken, phase, isFocused]);

    const resolveToken = useCallback(async () => {
        const existing = await tokenStorage.getToken();
        if (existing) return existing;
        return tokenStorage.ensureGuestToken();
    }, []);

    const handleAuthFailure = useCallback(async () => {
        await tokenStorage.clearToken();
        setAuthToken(null);
        setPhase('idle');
        navigation.navigate('LoginOptions');
    }, [navigation]);

    useEffect(() => {
        setOverlay((previousOverlay) => reconcileWithServer(previousOverlay, openRequestsData));
    }, [openRequestsData, openRequestsFulfilledTime]);

    useEffect(() => {
        if (!isFocused) return;
        setOverlay(createOverlayState());
    }, [isFocused]);

    useEffect(() => {
        const wasListening = previousListeningRef.current;
        const isListening = !!helperListening;
        previousListeningRef.current = isListening;
        if (wasListening && !isListening) setOverlay(createOverlayState());
    }, [helperListening]);

    const offer = useCallback(async (id, name) => {
        if (!authToken) return;
        const wasListeningAtTap = previousListeningRef.current;
        setOverlay((previousOverlay) => applyOverride(previousOverlay, id, 'offered'));
        try {
            const result = await createOffer({ authToken, chatGroupId: id }).unwrap();
            if (result && result.status === 'offered') {
                if (wasListeningAtTap && !previousListeningRef.current) {
                    setOverlay((previousOverlay) => removeOverride(previousOverlay, id));
                    try {
                        await withdrawOffer({ authToken, chatGroupId: id }).unwrap();
                    } catch (error) {
                    }
                    return;
                }
                setOverlay((previousOverlay) => settleOverride(previousOverlay, id));
                return;
            }
            setOverlay((previousOverlay) => removeOverride(previousOverlay, id));
            showToast('That request is no longer open', 'info');
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setOverlay((previousOverlay) => removeOverride(previousOverlay, id));
        }
    }, [authToken, createOffer, withdrawOffer, handleAuthFailure]);

    const withdraw = useCallback(async (id) => {
        if (!authToken) return;
        setOverlay((previousOverlay) => applyOverride(previousOverlay, id, 'none'));
        try {
            const result = await withdrawOffer({ authToken, chatGroupId: id }).unwrap();
            if (result && result.status === 'withdrawn') {
                setOverlay((previousOverlay) => settleOverride(previousOverlay, id));
                return;
            }
            setOverlay((previousOverlay) => removeOverride(previousOverlay, id));
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setOverlay((previousOverlay) => removeOverride(previousOverlay, id));
        }
    }, [authToken, withdrawOffer, handleAuthFailure]);

    const decline = useCallback(async (id) => {
        if (!authToken) return;
        setOverlay((previousOverlay) => hideRequest(previousOverlay, id));
        try {
            await declineOffer({ authToken, chatGroupId: id }).unwrap();
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setOverlay((previousOverlay) => unhideRequest(previousOverlay, id));
        }
    }, [authToken, declineOffer, handleAuthFailure]);

    const join = useCallback(async (id) => {
        if (!authToken) return;
        try {
            const result = await joinOffer({ authToken, chatGroupId: id }).unwrap();
            const status = result && result.status != null ? String(result.status).toLowerCase() : '';
            if (status === 'joined') {
                const targetId = (result && result.chatGroupId) || id;
                navigation.navigate('ChatGroup', { chatGroupId: targetId });
                return;
            }
            if (status === 'unavailable') {
                showToast('That conversation is no longer available', 'info');
            }
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
            }
        }
    }, [authToken, joinOffer, navigation, handleAuthFailure]);

    const declineInvite = useCallback(async (id) => {
        if (!authToken) return;
        setOverlay((previousOverlay) => hideStartedGroup(previousOverlay, id));
        try {
            await declineInviteRequest({ authToken, chatGroupId: id }).unwrap();
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setOverlay((previousOverlay) => unhideStartedGroup(previousOverlay, id));
        }
    }, [authToken, declineInviteRequest, handleAuthFailure]);

    useEffect(() => {
        let cancelled = false;
        const start = async () => {
            let token = null;
            try {
                token = await resolveToken();
            } catch (error) {
                token = null;
            }
            if (cancelled) return;
            if (!token) {
                setPhase('idle');
                return;
            }
            setAuthToken(token);
        };
        start();
        return () => {
            cancelled = true;
        };
    }, [resolveToken]);

    useEffect(() => {
        if (phase === 'loading' && (openRequestsData !== undefined || openRequestsError != null)) {
            setPhase('browsing');
        }
    }, [phase, openRequestsData, openRequestsError]);

    useEffect(() => {
        if (phase === 'idle' || !openRequestsError) return;
        if (openRequestsError.status === 401) {
            handleAuthFailure();
        }
    }, [phase, openRequestsError, handleAuthFailure]);

    const openRequests = projectRequests(overlay, openRequestsData);
    const startedGroups = projectStartedGroups(overlay, startedData);

    return { phase, startedGroups, openRequests, offer, withdraw, decline, join, declineInvite };
}