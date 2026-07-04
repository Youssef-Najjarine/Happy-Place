import { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigation } from '@react-navigation/native';
import tokenStorage from 'src/services/tokenStorage';
import authenticationService from 'src/services/authenticationService';
import helpSessionStorage from 'src/services/helpSessionStorage';
import { showToast } from 'src/components/Toast';
import { usePollRequestQuery, useLazyMyOpenRequestQuery, useCreateRequestMutation, useConnectMutation, useCancelRequestMutation } from 'src/store/helpApi';

const POLL_INTERVAL_MS = 3000;

export default function useSeekerSearch() {
    const navigation = useNavigation();
    const [phase, setPhase] = useState('idle');
    const [authToken, setAuthToken] = useState(null);
    const [chatGroupId, setChatGroupId] = useState(null);
    const [topic, setTopic] = useState(null);
    const [readyHelperCount, setReadyHelperCount] = useState(0);
    const navigatedRef = useRef(false);
    const searchGenRef = useRef(0);
    const [createRequest] = useCreateRequestMutation();
    const [connectRequest] = useConnectMutation();
    const [cancelRequest] = useCancelRequestMutation();
    const [triggerMyOpenRequest] = useLazyMyOpenRequestQuery();

    const { currentData: statusData, error: statusError } = usePollRequestQuery(
        { authToken, chatGroupId },
        { skip: phase !== 'waiting' || !authToken || !chatGroupId, pollingInterval: POLL_INTERVAL_MS }
    );

    const reset = useCallback(async () => {
        searchGenRef.current += 1;
        setPhase('idle');
        setChatGroupId(null);
        setTopic(null);
        setReadyHelperCount(0);
        await helpSessionStorage.clear();
    }, []);

    const goToChat = useCallback((id) => {
        if (navigatedRef.current) return;
        navigatedRef.current = true;
        searchGenRef.current += 1;
        setPhase('idle');
        setReadyHelperCount(0);
        helpSessionStorage.clear();
        navigation.navigate('ChatGroup', { chatGroupId: id });
    }, [navigation]);

    const resolveToken = useCallback(async () => {
        const existing = await tokenStorage.getToken();
        if (existing) {
            const validation = await authenticationService.validateToken(existing);
            if (validation.ok) return existing;
            await tokenStorage.clearToken();
        }
        const response = await authenticationService.createGuest();
        if (!response.ok) return null;
        const data = await response.json();
        await tokenStorage.saveToken(data.authToken);
        return data.authToken;
    }, []);

    const handleAuthFailure = useCallback(async () => {
        await tokenStorage.clearToken();
        await reset();
        setAuthToken(null);
        navigation.navigate('LoginOptions');
    }, [navigation, reset]);

    const beginSearch = useCallback(async (topicText) => {
        const gen = searchGenRef.current + 1;
        searchGenRef.current = gen;
        navigatedRef.current = false;
        setReadyHelperCount(0);
        setChatGroupId(null);
        setTopic(topicText);
        setPhase('waiting');
        let token = null;
        try {
            token = await resolveToken();
        } catch (error) {
            token = null;
        }
        if (searchGenRef.current !== gen) return;
        if (!token) {
            await reset();
            showToast('Couldn\u2019t reach the server \u2014 tap to try again', 'info');
            return;
        }
        setAuthToken(token);
        try {
            const result = await createRequest({ authToken: token, topic: topicText }).unwrap();
            if (searchGenRef.current !== gen) {
                if (result && result.status === 'waiting' && result.chatGroupId) {
                    cancelRequest({ authToken: token, chatGroupId: result.chatGroupId }).unwrap().catch(() => {});
                }
                return;
            }
            if (result.status === 'waiting') {
                await helpSessionStorage.saveSeeking(result.chatGroupId);
                setChatGroupId(result.chatGroupId);
                setTopic(result.chatGroupName);
                return;
            }
            await reset();
            showToast('Couldn\u2019t reach the server \u2014 tap to try again', 'info');
        } catch (error) {
            if (searchGenRef.current !== gen) return;
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            await reset();
            showToast('Couldn\u2019t reach the server \u2014 tap to try again', 'info');
        }
    }, [resolveToken, createRequest, cancelRequest, reset, handleAuthFailure]);

    const connect = useCallback(async () => {
        if (phase !== 'waiting' || !authToken || !chatGroupId) return;
        const gen = searchGenRef.current;
        setPhase('connecting');
        try {
            const result = await connectRequest({ authToken, chatGroupId }).unwrap();
            if (searchGenRef.current !== gen) return;
            if (result.status === 'connected') {
                goToChat(result.chatGroupId);
                return;
            }
            if (result.status === 'noOffers') {
                setPhase('waiting');
                return;
            }
            await reset();
        } catch (error) {
            if (searchGenRef.current !== gen) return;
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setPhase('waiting');
        }
    }, [phase, authToken, chatGroupId, connectRequest, goToChat, reset, handleAuthFailure]);

    const cancelSearch = useCallback(async () => {
        const token = authToken;
        const id = chatGroupId;
        await reset();
        if (token && id) {
            try {
                await cancelRequest({ authToken: token, chatGroupId: id }).unwrap();
            } catch (error) {
            }
        }
    }, [authToken, chatGroupId, cancelRequest, reset]);

    useEffect(() => {
        let cancelled = false;
        const restore = async () => {
            const token = await tokenStorage.getToken();
            if (cancelled || !token) return;
            const session = await helpSessionStorage.get();
            if (!cancelled && session && session.mode === 'seeking' && session.chatGroupId) {
                const validation = await authenticationService.validateToken(token);
                if (cancelled || !validation.ok) return;
                navigatedRef.current = false;
                setAuthToken(token);
                setChatGroupId(session.chatGroupId);
                setPhase('waiting');
                return;
            }
            try {
                const result = await triggerMyOpenRequest(token).unwrap();
                if (cancelled || !result || result.status !== 'waiting') return;
                navigatedRef.current = false;
                await helpSessionStorage.saveSeeking(result.chatGroupId);
                setAuthToken(token);
                setChatGroupId(result.chatGroupId);
                setPhase('waiting');
            } catch (error) {
            }
        };
        restore();
        return () => {
            cancelled = true;
        };
    }, [triggerMyOpenRequest]);

    useEffect(() => {
        if (phase !== 'waiting' || !statusData) return;
        if (statusData.status === 'connected') {
            goToChat(statusData.chatGroupId);
            return;
        }
        if (statusData.status === 'none') {
            reset();
            return;
        }
        if (statusData.status === 'waiting') {
            setReadyHelperCount(statusData.readyHelperCount);
        }
    }, [phase, statusData, goToChat, reset]);

    useEffect(() => {
        if (phase !== 'waiting' || !statusError) return;
        if (statusError.status === 401) {
            handleAuthFailure();
        }
    }, [phase, statusError, handleAuthFailure]);

    return { phase, readyHelperCount, topic, beginSearch, connect, cancelSearch };
}