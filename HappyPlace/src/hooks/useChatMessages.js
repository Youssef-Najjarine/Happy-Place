import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { AppState } from 'react-native';
import baseService from 'src/services/baseService';
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

function createClientMessageId() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (character) => {
        const randomNibble = (Math.random() * 16) | 0;
        const value = character === 'x' ? randomNibble : (randomNibble & 0x3) | 0x8;
        return value.toString(16);
    });
}

function upsertEntries(byId, entries) {
    const next = { ...byId };
    entries.forEach((entry) => {
        next[entry.id] = entry;
    });
    return next;
}

function mergeSenders(sendersById, senders) {
    if (!senders || senders.length === 0) return sendersById;
    const next = { ...sendersById };
    senders.forEach((sender) => {
        next[sender.id] = sender;
    });
    return next;
}

export default function useChatMessages({ authToken, chatGroupId, focused }) {
    const [status, setStatus] = useState('loading');
    const [messagesById, setMessagesById] = useState({});
    const [pendingMessages, setPendingMessages] = useState([]);
    const [sendersById, setSendersById] = useState({});
    const [readPointers, setReadPointers] = useState([]);
    const [typingUserIds, setTypingUserIds] = useState([]);
    const [callerUserAccountId, setCallerUserAccountId] = useState(null);
    const [nextCursor, setNextCursor] = useState(null);
    const [loadingOlder, setLoadingOlder] = useState(false);

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
    }, []);

    const loadFirstPage = useCallback(async () => {
        if (!authToken || !chatGroupId) return;
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
            setNextCursor(response.nextCursor);
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

    useEffect(() => {
        if (status !== 'ok' || !focused || !authToken) return undefined;
        const interval = setInterval(async () => {
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
                }
                applySharedBlocks(response);
            } catch (error) {
            } finally {
                pollBusyRef.current = false;
            }
        }, POLL_INTERVAL_MS);
        return () => clearInterval(interval);
    }, [status, focused, authToken, chatGroupId, triggerPoll, applySharedBlocks]);

    const orderedMessages = useMemo(() => {
        const serverMessages = Object.values(messagesById).sort((first, second) => first.sequence - second.sequence);
        return [...serverMessages, ...pendingMessages];
    }, [messagesById, pendingMessages]);

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

    const send = useCallback(async (body) => {
        const clientMessageId = createClientMessageId();
        const pendingEntry = {
            id: 'pending-' + clientMessageId,
            sequence: null,
            senderUserAccountId: callerUserAccountId,
            kind: 1,
            body,
            isDeleted: false,
            reactions: [],
            mediaUrl: null,
            mediaWidth: null,
            mediaHeight: null,
            mediaDurationSeconds: null,
            createdAtUtc: new Date().toISOString(),
            pending: true,
        };
        setPendingMessages((current) => [...current, pendingEntry]);
        try {
            const response = await sendChatMessage({ authToken, chatGroupId, clientMessageId, body }).unwrap();
            setPendingMessages((current) => current.filter((entry) => entry.id !== pendingEntry.id));
            if (response.status === 'sent' || response.status === 'duplicate') {
                setMessagesById((current) => upsertEntries(current, [response.message]));
                return { ok: true };
            }
            setStatus((current) => (response.status === 'notMember' || response.status === 'groupGone' ? response.status : current));
            return { ok: false, status: response.status };
        } catch (error) {
            setPendingMessages((current) => current.filter((entry) => entry.id !== pendingEntry.id));
            return { ok: false, status: 'unreachable' };
        }
    }, [authToken, chatGroupId, callerUserAccountId, sendChatMessage]);

    const sendMedia = useCallback(async (kind, file, durationSeconds) => {
        const clientMessageId = createClientMessageId();
        const pendingEntry = {
            id: 'pending-' + clientMessageId,
            sequence: null,
            senderUserAccountId: callerUserAccountId,
            kind,
            body: null,
            isDeleted: false,
            reactions: [],
            mediaUrl: null,
            mediaWidth: file.width || null,
            mediaHeight: file.height || null,
            mediaDurationSeconds: durationSeconds || null,
            localUri: kind === 2 ? file.uri : null,
            createdAtUtc: new Date().toISOString(),
            pending: true,
        };
        setPendingMessages((current) => [...current, pendingEntry]);
        const removePending = () => setPendingMessages((current) => current.filter((entry) => entry.id !== pendingEntry.id));
        try {
            const formData = new FormData();
            formData.append('AuthToken', authToken);
            formData.append('ChatGroupId', chatGroupId);
            formData.append('Kind', String(kind));
            formData.append('DurationSeconds', String(durationSeconds || 0));
            formData.append('Media', { uri: file.uri, type: file.type, name: file.name });
            const uploadResponse = await baseService.postMultipart('chatMedia/upload', formData);
            const uploadText = await uploadResponse.text();
            const upload = uploadText ? JSON.parse(uploadText) : null;
            if (!uploadResponse.ok || !upload || upload.status !== 'uploaded') {
                removePending();
                return { ok: false, status: upload && upload.status ? upload.status : 'unreachable' };
            }
            const response = await sendChatMessage({ authToken, chatGroupId, clientMessageId, mediaId: upload.mediaId }).unwrap();
            removePending();
            if (response.status === 'sent' || response.status === 'duplicate') {
                setMessagesById((current) => upsertEntries(current, [response.message]));
                return { ok: true };
            }
            setStatus((current) => (response.status === 'notMember' || response.status === 'groupGone' ? response.status : current));
            return { ok: false, status: response.status };
        } catch (error) {
            removePending();
            return { ok: false, status: 'unreachable' };
        }
    }, [authToken, chatGroupId, callerUserAccountId, sendChatMessage]);

    const sendImage = useCallback((file) => sendMedia(2, { ...file, type: file.type || 'image/jpeg', name: file.name || 'photo.jpg' }, 0), [sendMedia]);

    const sendVideo = useCallback((file, durationSeconds) => sendMedia(3, { ...file, type: file.type || 'video/mp4', name: file.name || 'video.mp4' }, durationSeconds), [sendMedia]);

    const sendVoice = useCallback((file, durationSeconds) => sendMedia(4, { ...file, type: file.type || 'audio/mp4', name: file.name || 'voice.m4a' }, durationSeconds), [sendMedia]);

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
        notifyTyping,
        isViewedByEveryoneElse,
        reload: loadFirstPage,
    };
}