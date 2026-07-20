import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { AppState } from 'react-native';
import baseService from 'src/services/baseService';
import { createClientMessageId, upsertEntries, mergeSenders, mergeMemberSenders, createPendingEntry, markPendingFailed, markPendingRetrying, setPendingMediaId, removePendingById, reconcilePending, orderMessages } from 'src/utils/chatMessageStore';
import {
    useLazyListMessagesPageQuery,
    useLazyPollMessagesQuery,
    useSendChatMessageMutation,
    useMarkMessagesReadMutation,
    useSendTypingPingMutation,
    useReactToMessageMutation,
    useDeleteOwnMessageMutation,
    useReportMessageMutation,
} from 'src/store/chatMessagesApi';

const POLL_INTERVAL_MS = 2000;
const TYPING_PING_INTERVAL_MS = 3000;
const MARK_READ_DEBOUNCE_MS = 600;

export default function useChatMessages({ authToken, chatGroupId, focused }) {
    const [status, setStatus] = useState('loading');
    const [messagesById, setMessagesById] = useState({});
    const [pendingMessages, setPendingMessages] = useState([]);
    const [sendersById, setSendersById] = useState({});
    const [readPointers, setReadPointers] = useState([]);
    const [typingUserIds, setTypingUserIds] = useState([]);
    const [groupState, setGroupState] = useState(null);
    const [callerUserAccountId, setCallerUserAccountId] = useState(null);
    const [nextCursor, setNextCursor] = useState(null);
    const [loadingOlder, setLoadingOlder] = useState(false);
    const [guestMessagesRemaining, setGuestMessagesRemaining] = useState(null);

    const watermarkRef = useRef(0);
    const pollBusyRef = useRef(false);
    const loadedRef = useRef(false);
    const typingLastSentAtRef = useRef(0);
    const markReadLastRef = useRef(0);
    const markReadTimerRef = useRef(null);
    const appActiveRef = useRef(true);

    const [triggerListPage] = useLazyListMessagesPageQuery();
    const [triggerPoll] = useLazyPollMessagesQuery();
    const [sendChatMessage] = useSendChatMessageMutation();
    const [markMessagesRead] = useMarkMessagesReadMutation();
    const [sendTypingPing] = useSendTypingPingMutation();
    const [reactToMessage] = useReactToMessageMutation();
    const [deleteOwnMessage] = useDeleteOwnMessageMutation();
    const [reportMessage] = useReportMessageMutation();

    const applySharedBlocks = useCallback((response) => {
        setSendersById((current) => mergeSenders(current, response.senders));
        if (Array.isArray(response.readPointers)) setReadPointers(response.readPointers);
        if (Array.isArray(response.typing)) setTypingUserIds(response.typing);
        if (response.group) {
            setGroupState(response.group);
            if (Array.isArray(response.group.members))
                setSendersById((current) => mergeMemberSenders(current, response.group.members));
        }
    }, []);

    const loadFirstPage = useCallback(async () => {
        if (!chatGroupId) {
            setStatus('groupGone');
            return;
        }
        if (!authToken) return;
        try {
            const response = await triggerListPage({ authToken, chatGroupId }).unwrap();
            if (response.status !== 'ok') {
                setStatus(response.status);
                return;
            }
            loadedRef.current = true;
            watermarkRef.current = response.changeSequence;
            setCallerUserAccountId(response.callerUserAccountId);
            setMessagesById(upsertEntries({}, response.items || []));
            setPendingMessages((current) => reconcilePending(current, response.items || []));
            setNextCursor(response.nextCursor);
            setGuestMessagesRemaining(response.guestMessagesRemaining == null ? null : response.guestMessagesRemaining);
            applySharedBlocks(response);
            setStatus('ok');
        } catch (error) {
            setStatus('unreachable');
        }
    }, [authToken, chatGroupId, triggerListPage, applySharedBlocks]);

    useEffect(() => {
        loadedRef.current = false;
        setStatus('loading');
        setMessagesById({});
        setPendingMessages([]);
        setGroupState(null);
        watermarkRef.current = 0;
        markReadLastRef.current = 0;
        loadFirstPage();
    }, [loadFirstPage]);

    const loadOlder = useCallback(async () => {
        if (!nextCursor || loadingOlder || status !== 'ok') return;
        setLoadingOlder(true);
        try {
            const response = await triggerListPage({ authToken, chatGroupId, cursor: nextCursor }).unwrap();
            if (response.status !== 'ok') {
                setStatus(response.status);
                return;
            }
            setMessagesById((current) => upsertEntries(current, response.items || []));
            setPendingMessages((current) => reconcilePending(current, response.items || []));
            setNextCursor(response.nextCursor);
            setSendersById((current) => mergeSenders(current, response.senders));
        } catch (error) {
        } finally {
            setLoadingOlder(false);
        }
    }, [authToken, chatGroupId, nextCursor, loadingOlder, status, triggerListPage]);

    useEffect(() => {
        const subscription = AppState.addEventListener('change', (nextState) => {
            appActiveRef.current = nextState === 'active';
        });
        return () => subscription.remove();
    }, []);

    const pollOnce = useCallback(async () => {
        if (pollBusyRef.current || !appActiveRef.current || !loadedRef.current) return;
        pollBusyRef.current = true;
        try {
            const response = await triggerPoll({ authToken, chatGroupId, sinceChangeSequence: watermarkRef.current }).unwrap();
            if (response.status !== 'ok') {
                setStatus(response.status);
                return;
            }
            watermarkRef.current = response.changeSequence;
            if (Array.isArray(response.changes) && response.changes.length > 0) {
                setMessagesById((current) => upsertEntries(current, response.changes));
                setPendingMessages((current) => reconcilePending(current, response.changes));
            }
            applySharedBlocks(response);
        } catch (error) {
        } finally {
            pollBusyRef.current = false;
        }
    }, [authToken, chatGroupId, triggerPoll, applySharedBlocks]);

    useEffect(() => {
        if (status !== 'ok' || !focused || !authToken) return undefined;
        pollOnce();
        const interval = setInterval(pollOnce, POLL_INTERVAL_MS);
        return () => clearInterval(interval);
    }, [status, focused, authToken, pollOnce]);

    const orderedMessages = useMemo(() => orderMessages(messagesById, pendingMessages), [messagesById, pendingMessages]);

    const newestSequence = useMemo(() => {
        let newest = 0;
        Object.values(messagesById).forEach((entry) => {
            if (entry.sequence > newest) newest = entry.sequence;
        });
        return newest;
    }, [messagesById]);

    useEffect(() => {
        if (status !== 'ok' || !focused || newestSequence <= markReadLastRef.current) return undefined;
        if (markReadTimerRef.current) clearTimeout(markReadTimerRef.current);
        markReadTimerRef.current = setTimeout(() => {
            const upToSequence = newestSequence;
            markReadLastRef.current = upToSequence;
            markMessagesRead({ authToken, chatGroupId, upToSequence })
                .unwrap()
                .then(() => {
                    setReadPointers((current) => current.map((pointer) =>
                        pointer.userAccountId === callerUserAccountId && pointer.lastReadSequence < upToSequence
                            ? { ...pointer, lastReadSequence: upToSequence }
                            : pointer));
                })
                .catch(() => {
                    markReadLastRef.current = 0;
                });
        }, MARK_READ_DEBOUNCE_MS);
        return () => {
            if (markReadTimerRef.current) clearTimeout(markReadTimerRef.current);
        };
    }, [status, focused, newestSequence, authToken, chatGroupId, callerUserAccountId, markMessagesRead]);

    const notifyTyping = useCallback(() => {
        const now = Date.now();
        if (now - typingLastSentAtRef.current < TYPING_PING_INTERVAL_MS) return;
        typingLastSentAtRef.current = now;
        sendTypingPing({ authToken, chatGroupId }).unwrap().catch(() => {});
    }, [authToken, chatGroupId, sendTypingPing]);

    const applySendOutcome = useCallback((pendingId, response) => {
        if (response.status === 'sent' || response.status === 'duplicate') {
            setPendingMessages((current) => removePendingById(current, pendingId));
            setMessagesById((current) => upsertEntries(current, [response.message]));
            setGuestMessagesRemaining(response.guestMessagesRemaining == null ? null : response.guestMessagesRemaining);
            return { ok: true };
        }
        if (response.status === 'guestLimitReached') {
            setPendingMessages((current) => removePendingById(current, pendingId));
            setGuestMessagesRemaining(0);
            return { ok: false, status: response.status };
        }
        if (response.status === 'notFriends') {
            setPendingMessages((current) => markPendingFailed(current, pendingId));
            return { ok: false, status: response.status };
        }
        if (response.status === 'notMember' || response.status === 'groupGone') {
            setPendingMessages((current) => removePendingById(current, pendingId));
            setStatus(response.status);
            return { ok: false, status: response.status };
        }
        setPendingMessages((current) => removePendingById(current, pendingId));
        return { ok: false, status: response.status };
    }, []);

    const attemptDelivery = useCallback(async (pendingEntry) => {
        const pendingId = pendingEntry.id;
        const payload = pendingEntry.retryPayload;
        try {
            if (payload.body !== undefined) {
                const response = await sendChatMessage({ authToken, chatGroupId, clientMessageId: pendingEntry.clientMessageId, body: payload.body, replyToMessageId: payload.replyToMessageId }).unwrap();
                return applySendOutcome(pendingId, response);
            }
            let mediaId = payload.mediaId;
            if (!mediaId) {
                const formData = new FormData();
                formData.append('AuthToken', authToken);
                formData.append('ChatGroupId', chatGroupId);
                formData.append('Kind', String(pendingEntry.kind));
                formData.append('DurationSeconds', String(payload.durationSeconds || 0));
                formData.append('Media', { uri: payload.file.uri, type: payload.file.type, name: payload.file.name });
                const uploadResponse = await baseService.postMultipart('chatMedia/upload', formData);
                const uploadText = await uploadResponse.text();
                const upload = uploadText ? JSON.parse(uploadText) : null;
                if (!uploadResponse.ok || !upload || upload.status !== 'uploaded') {
                    const uploadStatus = upload && upload.status ? upload.status : 'unreachable';
                    if (uploadStatus === 'unreachable') {
                        setPendingMessages((current) => markPendingFailed(current, pendingId));
                        return { ok: false, status: uploadStatus };
                    }
                    setPendingMessages((current) => removePendingById(current, pendingId));
                    setStatus((current) => (uploadStatus === 'notMember' || uploadStatus === 'groupGone' ? uploadStatus : current));
                    return { ok: false, status: uploadStatus };
                }
                mediaId = upload.mediaId;
                setPendingMessages((current) => setPendingMediaId(current, pendingId, mediaId));
            }
            const response = await sendChatMessage({ authToken, chatGroupId, clientMessageId: pendingEntry.clientMessageId, mediaId, replyToMessageId: payload.replyToMessageId }).unwrap();
            return applySendOutcome(pendingId, response);
        } catch (error) {
            setPendingMessages((current) => markPendingFailed(current, pendingId));
            return { ok: false, status: 'unreachable' };
        }
    }, [authToken, chatGroupId, sendChatMessage, applySendOutcome]);

    const send = useCallback(async (body, replyTo) => {
        const pendingEntry = createPendingEntry({ clientMessageId: createClientMessageId(), callerUserAccountId, kind: 1, body, replyTo, nowIso: new Date().toISOString() });
        setPendingMessages((current) => [...current, pendingEntry]);
        return attemptDelivery(pendingEntry);
    }, [callerUserAccountId, attemptDelivery]);

    const sendMedia = useCallback(async (kind, file, durationSeconds, replyTo) => {
        const pendingEntry = createPendingEntry({ clientMessageId: createClientMessageId(), callerUserAccountId, kind, file, durationSeconds, replyTo, nowIso: new Date().toISOString() });
        setPendingMessages((current) => [...current, pendingEntry]);
        return attemptDelivery(pendingEntry);
    }, [callerUserAccountId, attemptDelivery]);

    const retrySend = useCallback(async (messageId) => {
        const pendingEntry = pendingMessages.find((entry) => entry.id === messageId);
        if (!pendingEntry || !pendingEntry.failed) return { ok: false, status: 'missing' };
        setPendingMessages((current) => markPendingRetrying(current, messageId));
        return attemptDelivery(pendingEntry);
    }, [pendingMessages, attemptDelivery]);

    const deleteFailed = useCallback((messageId) => {
        setPendingMessages((current) => removePendingById(current, messageId));
    }, []);

    const sendImage = useCallback((file, replyTo) => sendMedia(2, { ...file, type: file.type || 'image/jpeg', name: file.name || 'photo.jpg' }, 0, replyTo), [sendMedia]);

    const sendVideo = useCallback((file, durationSeconds, replyTo) => sendMedia(3, { ...file, type: file.type || 'video/mp4', name: file.name || 'video.mp4' }, durationSeconds, replyTo), [sendMedia]);

    const sendVoice = useCallback((file, durationSeconds, replyTo) => sendMedia(4, { ...file, type: file.type || 'audio/mp4', name: file.name || 'voice.m4a' }, durationSeconds, replyTo), [sendMedia]);

    const reactTo = useCallback(async (messageId, emoji) => {
        setMessagesById((current) => {
            const entry = current[messageId];
            if (!entry) return current;
            const withoutMine = (entry.reactions || []).filter((reaction) => reaction.userAccountId !== callerUserAccountId);
            const nextReactions = !emoji ? withoutMine : [...withoutMine, { userAccountId: callerUserAccountId, emoji }];
            return { ...current, [messageId]: { ...entry, reactions: nextReactions } };
        });
        try {
            const response = await reactToMessage({ authToken, chatGroupId, messageId, emoji }).unwrap();
            return { ok: response.status === 'reacted' || response.status === 'removed', status: response.status };
        } catch (error) {
            return { ok: false, status: 'unreachable' };
        }
    }, [authToken, chatGroupId, callerUserAccountId, reactToMessage]);

    const deleteOwn = useCallback(async (messageId) => {
        setMessagesById((current) => {
            const entry = current[messageId];
            if (!entry) return current;
            return { ...current, [messageId]: { ...entry, isDeleted: true, body: null, mediaUrl: null, reactions: [] } };
        });
        try {
            const response = await deleteOwnMessage({ authToken, chatGroupId, messageId }).unwrap();
            return { ok: response.status === 'deleted', status: response.status };
        } catch (error) {
            return { ok: false, status: 'unreachable' };
        }
    }, [authToken, chatGroupId, deleteOwnMessage]);

    const report = useCallback(async (messageId, reason) => {
        try {
            const response = await reportMessage({ authToken, chatGroupId, messageId, reason }).unwrap();
            return { ok: response.status === 'reported', status: response.status };
        } catch (error) {
            return { ok: false, status: 'unreachable' };
        }
    }, [authToken, chatGroupId, reportMessage]);

    const isViewedByEveryoneElse = useCallback((sequence) => {
        if (!sequence || !callerUserAccountId) return false;
        const others = readPointers.filter((pointer) => pointer.userAccountId !== callerUserAccountId);
        if (others.length === 0) return false;
        return others.every((pointer) => pointer.lastReadSequence >= sequence);
    }, [readPointers, callerUserAccountId]);

    return {
        status,
        orderedMessages,
        sendersById,
        callerUserAccountId,
        typingUserIds,
        groupState,
        guestMessagesRemaining,
        hasOlder: !!nextCursor,
        loadingOlder,
        loadOlder,
        send,
        sendImage,
        sendVideo,
        sendVoice,
        reactTo,
        deleteOwn,
        report,
        retrySend,
        deleteFailed,
        notifyTyping,
        isViewedByEveryoneElse,
        reload: loadFirstPage,
        refreshNow: pollOnce,
    };
}