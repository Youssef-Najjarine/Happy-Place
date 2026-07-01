import { useState, useEffect, useCallback } from 'react';
import { useNavigation, useIsFocused } from '@react-navigation/native';
import tokenStorage from 'src/services/tokenStorage';
import authenticationService from 'src/services/authenticationService';
import { useOpenRequestsQuery, usePollOfferQuery, useCreateOfferMutation, useWithdrawOfferMutation, useDeclineOfferMutation, useJoinMutation, useDeclineInviteMutation } from 'src/store/helpApi';

const BROWSE_INTERVAL_MS = 3000;
const READY_INTERVAL_MS = 3000;

export default function useHelperOffer() {
    const navigation = useNavigation();
    const isFocused = useIsFocused();
    const [phase, setPhase] = useState('loading');
    const [authToken, setAuthToken] = useState(null);
    const [overrides, setOverrides] = useState({});
    const [hidden, setHidden] = useState({});
    const [hiddenStarted, setHiddenStarted] = useState({});
    const [createOffer] = useCreateOfferMutation();
    const [withdrawOffer] = useWithdrawOfferMutation();
    const [declineOffer] = useDeclineOfferMutation();
    const [joinOffer] = useJoinMutation();
    const [declineInviteRequest] = useDeclineInviteMutation();

    const { data: openRequestsData, error: openRequestsError } = useOpenRequestsQuery(
        authToken,
        { skip: !authToken || phase === 'idle' || !isFocused, pollingInterval: BROWSE_INTERVAL_MS, refetchOnMountOrArgChange: true }
    );

    const { data: startedData } = usePollOfferQuery(
        authToken,
        { skip: !authToken || phase === 'idle' || !isFocused, pollingInterval: READY_INTERVAL_MS }
    );

    const resolveToken = useCallback(async () => {
        const existing = await tokenStorage.getToken();
        if (existing) return existing;
        const response = await authenticationService.createGuest();
        if (!response.ok) return null;
        const data = await response.json();
        await tokenStorage.saveToken(data.authToken);
        return data.authToken;
    }, []);

    const handleAuthFailure = useCallback(async () => {
        await tokenStorage.clearToken();
        setAuthToken(null);
        setPhase('idle');
        navigation.navigate('LoginOptions');
    }, [navigation]);

    const offer = useCallback(async (id, name) => {
        if (!authToken) return;
        setOverrides((prev) => ({ ...prev, [id]: 'offered' }));
        try {
            await createOffer({ authToken, chatGroupId: id }).unwrap();
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setOverrides((prev) => ({ ...prev, [id]: 'none' }));
        }
    }, [authToken, createOffer, handleAuthFailure]);

    const withdraw = useCallback(async (id) => {
        if (!authToken) return;
        setOverrides((prev) => ({ ...prev, [id]: 'none' }));
        try {
            await withdrawOffer({ authToken, chatGroupId: id }).unwrap();
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setOverrides((prev) => ({ ...prev, [id]: 'offered' }));
        }
    }, [authToken, withdrawOffer, handleAuthFailure]);

    const decline = useCallback(async (id) => {
        if (!authToken) return;
        setHidden((prev) => ({ ...prev, [id]: true }));
        try {
            await declineOffer({ authToken, chatGroupId: id }).unwrap();
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setHidden((prev) => {
                const next = { ...prev };
                delete next[id];
                return next;
            });
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
            }
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
            }
        }
    }, [authToken, joinOffer, navigation, handleAuthFailure]);

    const declineInvite = useCallback(async (id) => {
        if (!authToken) return;
        setHiddenStarted((prev) => ({ ...prev, [id]: true }));
        try {
            await declineInviteRequest({ authToken, chatGroupId: id }).unwrap();
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setHiddenStarted((prev) => {
                const next = { ...prev };
                delete next[id];
                return next;
            });
        }
    }, [authToken, declineInviteRequest, handleAuthFailure]);

    useEffect(() => {
        let cancelled = false;
        const start = async () => {
            const token = await resolveToken();
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

    useEffect(() => {
        if (!isFocused) return;
        setOverrides({});
        setHidden({});
        setHiddenStarted({});
    }, [isFocused]);

    const raw = openRequestsData || [];
    const openRequests = raw
        .filter((item) => !hidden[item.chatGroupId])
        .map((item) => ({
            ...item,
            offerStatus: overrides[item.chatGroupId] != null ? overrides[item.chatGroupId] : item.offerStatus
        }));
    const startedGroups = (Array.isArray(startedData) ? startedData : []).filter((group) => !hiddenStarted[group.chatGroupId]);

    return { phase, startedGroups, openRequests, offer, withdraw, decline, join, declineInvite };
}