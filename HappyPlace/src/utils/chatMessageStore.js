export function createClientMessageId() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (character) => {
        const randomNibble = (Math.random() * 16) | 0;
        const value = character === 'x' ? randomNibble : (randomNibble & 0x3) | 0x8;
        return value.toString(16);
    });
}

export function upsertEntries(byId, entries) {
    const next = { ...byId };
    entries.forEach((entry) => {
        next[entry.id] = entry;
    });
    return next;
}

export function mergeSenders(sendersById, senders) {
    if (!senders || senders.length === 0) return sendersById;
    const next = { ...sendersById };
    senders.forEach((sender) => {
        next[sender.id] = sender;
    });
    return next;
}

export function mergeMemberSenders(sendersById, members) {
    if (!members || members.length === 0) return sendersById;
    let next = null;
    members.forEach((member) => {
        const existing = sendersById[member.userAccountId];
        if (existing && existing.displayName === member.name && existing.profilePhotoUrl === member.profilePhotoUrl && existing.avatarColor === member.avatarColor) return;
        if (!next) next = { ...sendersById };
        next[member.userAccountId] = { id: member.userAccountId, displayName: member.name, profilePhotoUrl: member.profilePhotoUrl, avatarColor: member.avatarColor };
    });
    return next || sendersById;
}

export function createPendingEntry({ clientMessageId, callerUserAccountId, kind, body, file, durationSeconds, replyTo, nowIso }) {
    const isMedia = kind !== 1;
    return {
        id: 'pending-' + clientMessageId,
        clientMessageId,
        sequence: null,
        senderUserAccountId: callerUserAccountId,
        kind,
        body: isMedia ? null : body,
        isDeleted: false,
        reactions: [],
        mediaUrl: null,
        mediaWidth: isMedia ? (file && file.width) || null : null,
        mediaHeight: isMedia ? (file && file.height) || null : null,
        mediaDurationSeconds: isMedia ? durationSeconds || null : null,
        localUri: kind === 2 && file ? file.uri : null,
        replyTo: replyTo || null,
        createdAtUtc: nowIso,
        pending: true,
        failed: false,
        retryPayload: isMedia
            ? { file: file ? { uri: file.uri, type: file.type, name: file.name, width: file.width || null, height: file.height || null } : null, durationSeconds: durationSeconds || 0, mediaId: null, replyToMessageId: replyTo ? replyTo.messageId : null }
            : { body, replyToMessageId: replyTo ? replyTo.messageId : null },
    };
}

export function markPendingFailed(pendingList, pendingId) {
    let changed = false;
    const next = pendingList.map((entry) => {
        if (entry.id !== pendingId || entry.failed) return entry;
        changed = true;
        return { ...entry, failed: true };
    });
    return changed ? next : pendingList;
}

export function markPendingRetrying(pendingList, pendingId) {
    let changed = false;
    const next = pendingList.map((entry) => {
        if (entry.id !== pendingId || !entry.failed) return entry;
        changed = true;
        return { ...entry, failed: false };
    });
    return changed ? next : pendingList;
}

export function setPendingMediaId(pendingList, pendingId, mediaId) {
    let changed = false;
    const next = pendingList.map((entry) => {
        if (entry.id !== pendingId || !entry.retryPayload) return entry;
        changed = true;
        return { ...entry, retryPayload: { ...entry.retryPayload, mediaId } };
    });
    return changed ? next : pendingList;
}

export function removePendingById(pendingList, pendingId) {
    const next = pendingList.filter((entry) => entry.id !== pendingId);
    return next.length === pendingList.length ? pendingList : next;
}

export function reconcilePending(pendingList, serverEntries) {
    if (pendingList.length === 0 || !serverEntries || serverEntries.length === 0) return pendingList;
    const confirmedClientIds = {};
    serverEntries.forEach((entry) => {
        if (entry.clientMessageId) confirmedClientIds[entry.clientMessageId] = true;
    });
    const next = pendingList.filter((entry) => !confirmedClientIds[entry.clientMessageId]);
    return next.length === pendingList.length ? pendingList : next;
}

export function orderMessages(messagesById, pendingMessages) {
    const serverMessages = Object.values(messagesById).sort((first, second) => first.sequence - second.sequence);
    return [...serverMessages, ...pendingMessages];
}

export function buildReplyContext(parentEntry, sendersById) {
    if (!parentEntry) return null;
    const sender = parentEntry.senderUserAccountId && sendersById ? sendersById[parentEntry.senderUserAccountId] : null;
    const previewSource = parentEntry.kind === 1 && !parentEntry.isDeleted ? parentEntry.body || '' : null;
    return {
        messageId: parentEntry.id,
        senderUserAccountId: parentEntry.senderUserAccountId || null,
        senderDisplayName: sender ? sender.displayName : null,
        kind: parentEntry.kind,
        preview: previewSource == null ? null : previewSource.slice(0, 140),
        isDeleted: !!parentEntry.isDeleted,
    };
}

export function resolveReplyDisplay(replyTo, messagesById, sendersById) {
    if (!replyTo) return null;
    const liveParent = messagesById ? messagesById[replyTo.messageId] : null;
    if (!liveParent) {
        return { messageId: replyTo.messageId, senderDisplayName: replyTo.senderDisplayName || null, kind: replyTo.kind, preview: replyTo.preview == null ? null : replyTo.preview, isDeleted: !!replyTo.isDeleted, parentIsLoaded: false };
    }
    const liveContext = buildReplyContext(liveParent, sendersById);
    return {
        messageId: liveContext.messageId,
        senderDisplayName: liveContext.senderDisplayName || replyTo.senderDisplayName || null,
        kind: liveContext.kind,
        preview: liveContext.preview,
        isDeleted: liveContext.isDeleted,
        parentIsLoaded: true,
    };
}