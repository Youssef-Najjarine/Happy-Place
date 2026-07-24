import { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigation } from '@react-navigation/native';
import { useSelector } from 'react-redux';
import tokenStorage from 'src/services/tokenStorage';
import authenticationService from 'src/services/authenticationService';
import helpSessionStorage from 'src/services/helpSessionStorage';
import { selectRealtimeConnected, selectHelpChangedTick } from 'src/store/realtimeSlice';
import { helpPollingInterval } from 'src/utils/pollingPolicy';
import { showToast } from 'src/components/Toast';
import { usePollRequestQuery, useLazyMyOpenRequestQuery, useCreateRequestMutation, useConnectMutation, useCancelRequestMutation } from 'src/store/helpApi';
import { useRenameChatGroupMutation } from 'src/store/chatGroupsApi';

const CANCEL_RETRY_BASE_MS = 1500;
const CANCEL_RETRY_MAX_MS = 15000;

export default function useSeekerSearch() {
    const navigation = useNavigation();
    const [phase, setPhase] = useState('idle');
    const [authToken, setAuthToken] = useState(null);
    const [chatGroupId, setChatGroupId] = useState(null);
    const [topic, setTopic] = useState(null);
    const [readyHelperCount, setReadyHelperCount] = useState(0);
    const navigatedRef = useRef(false);
    const searchGenRef = useRef(0);
    const chatGroupIdRef = useRef(null);
    const aliveRef = useRef(true);
    const [createRequest] = useCreateRequestMutation();
    const [connectRequest] = useConnectMutation();
    const [cancelRequest] = useCancelRequestMutation();
    const [triggerMyOpenRequest] = useLazyMyOpenRequestQuery();
    const [renameChatGroup] = useRenameChatGroupMutation();

    const isRealtimeConnected = useSelector(selectRealtimeConnected);
    const helpChangedTick = useSelector(selectHelpChangedTick);
    const previousHelpTickRef = useRef(helpChangedTick);

    useEffect(() => {
        aliveRef.current = true;
        return () => {
            aliveRef.current = false;
        };
    }, []);

    const { currentData: statusData, error: statusError, refetch: refetchStatus } = usePollRequestQuery(
        { authToken, chatGroupId },
        { skip: phase !== 'waiting' || !authToken || !chatGroupId, pollingInterval: helpPollingInterval(isRealtimeConnected) }
    );

    useEffect(() => {
        if (helpChangedTick === previousHelpTickRef.current) return;
        previousHelpTickRef.current = helpChangedTick;
        if (phase !== 'waiting' || !authToken || !chatGroupId) return;
        refetchStatus();
    }, [helpChangedTick, phase, authToken, chatGroupId, refetchStatus]);

    const reset = useCallback(async () => {
        searchGenRef.current += 1;
        chatGroupIdRef.current = null;
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
            if (validation.status !== 401) return existing;
            await tokenStorage.clearToken();
        }
        return tokenStorage.ensureGuestToken();
    }, []);

    const handleAuthFailure = useCallback(async () => {
        await tokenStorage.clearToken();
        await reset();
        setAuthToken(null);
        navigation.navigate('LoginOptions');
    }, [navigation, reset]);

    const waitBeforeRetry = useCallback((delayMs) => {
        return new Promise((resolve) => setTimeout(resolve, delayMs));
    }, []);

    const cancelRequestWithRetry = useCallback(async (token, targetChatGroupId) => {
        let retryDelayMs = CANCEL_RETRY_BASE_MS;
        while (aliveRef.current) {
            if (chatGroupIdRef.current === targetChatGroupId) return;
            try {
                await cancelRequest({ authToken: token, chatGroupId: targetChatGroupId }).unwrap();
                return;
            } catch (error) {
                if (error && error.status === 401) return;
                await waitBeforeRetry(retryDelayMs);
                retryDelayMs = Math.min(retryDelayMs * 2, CANCEL_RETRY_MAX_MS);
            }
        }
    }, [cancelRequest, waitBeforeRetry]);

    const beginSearch = useCallback(async (topicText) => {
        const gen = searchGenRef.current + 1;
        searchGenRef.current = gen;
        navigatedRef.current = false;
        setReadyHelperCount(0);
        chatGroupIdRef.current = null;
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
                    cancelRequestWithRetry(token, result.chatGroupId);
                }
                return;
            }
            if (result.status === 'waiting') {
                await helpSessionStorage.saveSeeking(token, result.chatGroupId);
                chatGroupIdRef.current = result.chatGroupId;
                setChatGroupId(result.chatGroupId);
                if (result.chatGroupName !== topicText) {
                    let resolvedTitle = result.chatGroupName;
                    try {
                        const renameResult = await renameChatGroup({ authToken: token, chatGroupId: result.chatGroupId, name: topicText }).unwrap();
                        if (renameResult && renameResult.status === 'renamed' && renameResult.title != null) resolvedTitle = renameResult.title;
                    } catch (error) {
                    }
                    if (searchGenRef.current !== gen) return;
                    setTopic(resolvedTitle);
                    return;
                }
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
    }, [resolveToken, createRequest, cancelRequestWithRetry, reset, handleAuthFailure]);

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
            showToast('Your request is no longer open', 'info');
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
        if (phase === 'connecting') return;
        const token = authToken;
        const targetChatGroupId = chatGroupId;
        await reset();
        if (token && targetChatGroupId) {
            await cancelRequestWithRetry(token, targetChatGroupId);
        }
    }, [phase, authToken, chatGroupId, reset, cancelRequestWithRetry]);

    const updateTopic = useCallback(async (newTopic) => {
        if (phase !== 'waiting' || !authToken || !chatGroupId) return;
        const previousTopic = topic;
        setTopic(newTopic);
        try {
            const result = await renameChatGroup({ authToken, chatGroupId, name: newTopic }).unwrap();
            if (result && result.status === 'renamed') {
                if (result.title != null) setTopic(result.title);
                return;
            }
            setTopic(previousTopic);
            showToast('Couldn\u2019t update that \u2014 tap to try again', 'info');
        } catch (error) {
            if (error && error.status === 401) {
                await handleAuthFailure();
                return;
            }
            setTopic(previousTopic);
            showToast('Couldn\u2019t update that \u2014 tap to try again', 'info');
        }
    }, [phase, authToken, chatGroupId, topic, renameChatGroup, handleAuthFailure]);

    useEffect(() => {
        let cancelled = false;
        const restore = async () => {
            const restoreGeneration = searchGenRef.current;
            const token = await tokenStorage.getToken();
            if (cancelled || searchGenRef.current !== restoreGeneration || !token) return;
            const session = await helpSessionStorage.get();
            if (cancelled || searchGenRef.current !== restoreGeneration) return;
            if (session && session.mode === 'seeking' && session.chatGroupId && session.ownerToken === token) {
                let validation = null;
                try {
                    validation = await authenticationService.validateToken(token);
                } catch (error) {
                    return;
                }
                if (cancelled || searchGenRef.current !== restoreGeneration || !validation.ok) return;
                navigatedRef.current = false;
                setAuthToken(token);
                chatGroupIdRef.current = session.chatGroupId;
                setChatGroupId(session.chatGroupId);
                setPhase('waiting');
                return;
            }
            try {
                const result = await triggerMyOpenRequest(token).unwrap();
                if (cancelled || searchGenRef.current !== restoreGeneration) return;
                if (!result || result.status !== 'waiting') return;
                navigatedRef.current = false;
                await helpSessionStorage.saveSeeking(token, result.chatGroupId);
                if (cancelled || searchGenRef.current !== restoreGeneration) return;
                setAuthToken(token);
                chatGroupIdRef.current = result.chatGroupId;
                setChatGroupId(result.chatGroupId);
                setTopic(result.chatGroupName);
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
            showToast('Your request is no longer open', 'info');
            return;
        }
        if (statusData.status === 'waiting') {
            setReadyHelperCount(statusData.readyHelperCount);
            setTopic(statusData.chatGroupName);
        }
    }, [phase, statusData, goToChat, reset]);

    useEffect(() => {
        if (phase !== 'waiting' || !statusError) return;
        if (statusError.status === 401) {
            handleAuthFailure();
        }
    }, [phase, statusError, handleAuthFailure]);

    return { phase, readyHelperCount, topic, beginSearch, connect, cancelSearch, updateTopic };
}