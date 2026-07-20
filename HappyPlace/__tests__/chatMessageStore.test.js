import {
    createClientMessageId,
    upsertEntries,
    mergeSenders,
    mergeMemberSenders,
    createPendingEntry,
    markPendingFailed,
    markPendingRetrying,
    setPendingMediaId,
    removePendingById,
    reconcilePending,
    orderMessages,
    buildReplyContext,
    resolveReplyDisplay,
} from '../src/utils/chatMessageStore';

function makeTextPending(clientMessageId, overrides) {
    return {
        ...createPendingEntry({ clientMessageId, callerUserAccountId: 'caller-1', kind: 1, body: 'hello', nowIso: '2026-07-16T01:00:00.000Z' }),
        ...(overrides || {}),
    };
}

function makeServerEntry(id, sequence, overrides) {
    return {
        id,
        clientMessageId: null,
        sequence,
        senderUserAccountId: 'caller-1',
        kind: 1,
        body: 'server body',
        isDeleted: false,
        reactions: [],
        mediaUrl: null,
        mediaWidth: null,
        mediaHeight: null,
        mediaDurationSeconds: null,
        replyTo: null,
        createdAtUtc: '2026-07-16T01:00:00Z',
        ...(overrides || {}),
    };
}

describe('createClientMessageId', () => {
    it('produces a v4 uuid shaped id', () => {
        expect(createClientMessageId()).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
    });

    it('produces unique ids across many calls', () => {
        const seen = new Set();
        for (let index = 0; index < 1000; index++) {
            seen.add(createClientMessageId());
        }
        expect(seen.size).toBe(1000);
    });
});

describe('createPendingEntry', () => {
    it('builds a text entry with every legacy field plus retry fields', () => {
        const entry = createPendingEntry({ clientMessageId: 'cid-1', callerUserAccountId: 'caller-1', kind: 1, body: 'hi', nowIso: '2026-07-16T01:00:00.000Z' });
        expect(entry.id).toBe('pending-cid-1');
        expect(entry.clientMessageId).toBe('cid-1');
        expect(entry.sequence).toBeNull();
        expect(entry.senderUserAccountId).toBe('caller-1');
        expect(entry.kind).toBe(1);
        expect(entry.body).toBe('hi');
        expect(entry.isDeleted).toBe(false);
        expect(entry.reactions).toEqual([]);
        expect(entry.mediaUrl).toBeNull();
        expect(entry.mediaWidth).toBeNull();
        expect(entry.mediaHeight).toBeNull();
        expect(entry.mediaDurationSeconds).toBeNull();
        expect(entry.localUri).toBeNull();
        expect(entry.createdAtUtc).toBe('2026-07-16T01:00:00.000Z');
        expect(entry.pending).toBe(true);
        expect(entry.failed).toBe(false);
        expect(entry.replyTo).toBeNull();
        expect(entry.retryPayload).toEqual({ body: 'hi', replyToMessageId: null });
    });

    it('builds an image entry with localUri, dimensions, and a media retry payload', () => {
        const file = { uri: 'file:///photo.jpg', type: 'image/jpeg', name: 'photo.jpg', width: 800, height: 600 };
        const entry = createPendingEntry({ clientMessageId: 'cid-2', callerUserAccountId: 'caller-1', kind: 2, file, durationSeconds: 0, nowIso: '2026-07-16T01:00:00.000Z' });
        expect(entry.kind).toBe(2);
        expect(entry.body).toBeNull();
        expect(entry.localUri).toBe('file:///photo.jpg');
        expect(entry.mediaWidth).toBe(800);
        expect(entry.mediaHeight).toBe(600);
        expect(entry.retryPayload.file).toEqual({ uri: 'file:///photo.jpg', type: 'image/jpeg', name: 'photo.jpg', width: 800, height: 600 });
        expect(entry.retryPayload.mediaId).toBeNull();
    });

    it('builds a video entry without localUri and with duration', () => {
        const file = { uri: 'file:///clip.mp4', type: 'video/mp4', name: 'video.mp4', width: 1920, height: 1080 };
        const entry = createPendingEntry({ clientMessageId: 'cid-3', callerUserAccountId: 'caller-1', kind: 3, file, durationSeconds: 12, nowIso: '2026-07-16T01:00:00.000Z' });
        expect(entry.localUri).toBeNull();
        expect(entry.mediaDurationSeconds).toBe(12);
        expect(entry.retryPayload.durationSeconds).toBe(12);
    });

    it('builds a voice entry without localUri and with duration', () => {
        const file = { uri: 'file:///note.m4a', type: 'audio/mp4', name: 'voice.m4a' };
        const entry = createPendingEntry({ clientMessageId: 'cid-4', callerUserAccountId: 'caller-1', kind: 4, file, durationSeconds: 7, nowIso: '2026-07-16T01:00:00.000Z' });
        expect(entry.localUri).toBeNull();
        expect(entry.mediaWidth).toBeNull();
        expect(entry.mediaDurationSeconds).toBe(7);
        expect(entry.retryPayload.durationSeconds).toBe(7);
    });

    it('carries a reply context and threads the parent id into the retry payload', () => {
        const replyTo = { messageId: 'm-parent', senderUserAccountId: 'caller-2', senderDisplayName: 'Sam', kind: 1, preview: 'the original', isDeleted: false };
        const entry = createPendingEntry({ clientMessageId: 'cid-5', callerUserAccountId: 'caller-1', kind: 1, body: 'the reply', replyTo, nowIso: '2026-07-19T01:00:00.000Z' });
        expect(entry.replyTo).toEqual(replyTo);
        expect(entry.retryPayload).toEqual({ body: 'the reply', replyToMessageId: 'm-parent' });
    });

    it('threads the reply link through a media retry payload', () => {
        const file = { uri: 'file:///photo.jpg', type: 'image/jpeg', name: 'photo.jpg', width: 800, height: 600 };
        const replyTo = { messageId: 'm-parent', senderUserAccountId: 'caller-2', senderDisplayName: 'Sam', kind: 1, preview: 'the original', isDeleted: false };
        const entry = createPendingEntry({ clientMessageId: 'cid-6', callerUserAccountId: 'caller-1', kind: 2, file, durationSeconds: 0, replyTo, nowIso: '2026-07-19T01:00:00.000Z' });
        expect(entry.replyTo).toEqual(replyTo);
        expect(entry.retryPayload.replyToMessageId).toBe('m-parent');
        expect(entry.retryPayload.mediaId).toBeNull();
    });
});

describe('upsertEntries', () => {
    it('inserts new entries keyed by id', () => {
        const next = upsertEntries({}, [makeServerEntry('m1', 1), makeServerEntry('m2', 2)]);
        expect(Object.keys(next)).toHaveLength(2);
        expect(next.m1.sequence).toBe(1);
    });

    it('replaces an existing entry with the same id', () => {
        const first = upsertEntries({}, [makeServerEntry('m1', 1, { body: 'old' })]);
        const next = upsertEntries(first, [makeServerEntry('m1', 1, { body: 'new' })]);
        expect(next.m1.body).toBe('new');
        expect(Object.keys(next)).toHaveLength(1);
    });

    it('does not mutate the input map', () => {
        const original = upsertEntries({}, [makeServerEntry('m1', 1)]);
        upsertEntries(original, [makeServerEntry('m2', 2)]);
        expect(Object.keys(original)).toHaveLength(1);
    });

    it('returns an equal map for an empty entry list', () => {
        const original = upsertEntries({}, [makeServerEntry('m1', 1)]);
        expect(upsertEntries(original, [])).toEqual(original);
    });
});

describe('mergeSenders', () => {
    it('merges new senders and replaces existing ones by id', () => {
        const first = mergeSenders({}, [{ id: 's1', displayName: 'Ann' }]);
        const next = mergeSenders(first, [{ id: 's1', displayName: 'Anne' }, { id: 's2', displayName: 'Bo' }]);
        expect(next.s1.displayName).toBe('Anne');
        expect(Object.keys(next)).toHaveLength(2);
    });

    it('returns the same reference for null or empty senders', () => {
        const current = { s1: { id: 's1', displayName: 'Ann' } };
        expect(mergeSenders(current, null)).toBe(current);
        expect(mergeSenders(current, [])).toBe(current);
    });
});

describe('mergeMemberSenders', () => {
    function makeMember(userAccountId, name, profilePhotoUrl) {
        return { userAccountId, name, profilePhotoUrl: profilePhotoUrl || null, username: 'user', avatarColor: '#E17055', isOwner: false };
    }

    it('refreshes a stale sender from live group-state members', () => {
        const current = { u1: { id: 'u1', displayName: 'Guest4821', profilePhotoUrl: null } };
        const next = mergeMemberSenders(current, [makeMember('u1', 'Casey Jordan', 'https://photos/u1.jpg')]);
        expect(next.u1).toEqual({ id: 'u1', displayName: 'Casey Jordan', profilePhotoUrl: 'https://photos/u1.jpg', avatarColor: '#E17055' });
    });

    it('adds members that were never in the senders map', () => {
        const next = mergeMemberSenders({}, [makeMember('u1', 'Casey', null)]);
        expect(next.u1.displayName).toBe('Casey');
    });

    it('returns the same reference when every member already matches', () => {
        const current = { u1: { id: 'u1', displayName: 'Casey', profilePhotoUrl: null, avatarColor: '#E17055' } };
        expect(mergeMemberSenders(current, [makeMember('u1', 'Casey', null)])).toBe(current);
        const recolored = mergeMemberSenders(current, [{ ...makeMember('u1', 'Casey', null), avatarColor: '#0984E3' }]);
        expect(recolored.u1.avatarColor).toBe('#0984E3');
        expect(current.u1.avatarColor).toBe('#E17055');
    });

    it('keeps former members untouched while refreshing current ones', () => {
        const current = { u1: { id: 'u1', displayName: 'Guest4821', profilePhotoUrl: null }, gone: { id: 'gone', displayName: 'Left The Group', profilePhotoUrl: null } };
        const next = mergeMemberSenders(current, [makeMember('u1', 'Casey', null)]);
        expect(next.gone.displayName).toBe('Left The Group');
        expect(next.u1.displayName).toBe('Casey');
    });

    it('returns the same reference for null or empty member lists and does not mutate inputs', () => {
        const current = { u1: { id: 'u1', displayName: 'Casey', profilePhotoUrl: null } };
        expect(mergeMemberSenders(current, null)).toBe(current);
        expect(mergeMemberSenders(current, [])).toBe(current);
        mergeMemberSenders(current, [makeMember('u1', 'Renamed', null)]);
        expect(current.u1.displayName).toBe('Casey');
    });
});

describe('markPendingFailed', () => {
    it('flags only the matching entry as failed', () => {
        const list = [makeTextPending('cid-1'), makeTextPending('cid-2')];
        const next = markPendingFailed(list, 'pending-cid-1');
        expect(next[0].failed).toBe(true);
        expect(next[1].failed).toBe(false);
    });

    it('keeps pending true so failed entries stay excluded from confirmed-only logic', () => {
        const next = markPendingFailed([makeTextPending('cid-1')], 'pending-cid-1');
        expect(next[0].pending).toBe(true);
    });

    it('returns the same reference when the id is absent', () => {
        const list = [makeTextPending('cid-1')];
        expect(markPendingFailed(list, 'pending-missing')).toBe(list);
    });

    it('returns the same reference when the entry is already failed', () => {
        const list = [makeTextPending('cid-1', { failed: true })];
        expect(markPendingFailed(list, 'pending-cid-1')).toBe(list);
    });

    it('does not mutate the input entries', () => {
        const list = [makeTextPending('cid-1')];
        markPendingFailed(list, 'pending-cid-1');
        expect(list[0].failed).toBe(false);
    });
});

describe('markPendingRetrying', () => {
    it('clears the failed flag on the matching entry', () => {
        const list = [makeTextPending('cid-1', { failed: true })];
        const next = markPendingRetrying(list, 'pending-cid-1');
        expect(next[0].failed).toBe(false);
    });

    it('returns the same reference when the entry is not failed', () => {
        const list = [makeTextPending('cid-1')];
        expect(markPendingRetrying(list, 'pending-cid-1')).toBe(list);
    });

    it('returns the same reference when the id is absent', () => {
        const list = [makeTextPending('cid-1', { failed: true })];
        expect(markPendingRetrying(list, 'pending-missing')).toBe(list);
    });
});

describe('setPendingMediaId', () => {
    it('captures the uploaded media id inside the retry payload', () => {
        const file = { uri: 'file:///photo.jpg', type: 'image/jpeg', name: 'photo.jpg' };
        const list = [createPendingEntry({ clientMessageId: 'cid-1', callerUserAccountId: 'caller-1', kind: 2, file, durationSeconds: 0, nowIso: '2026-07-16T01:00:00.000Z' })];
        const next = setPendingMediaId(list, 'pending-cid-1', 'media-9');
        expect(next[0].retryPayload.mediaId).toBe('media-9');
        expect(next[0].retryPayload.file.uri).toBe('file:///photo.jpg');
    });

    it('does not mutate the original payload', () => {
        const file = { uri: 'file:///photo.jpg', type: 'image/jpeg', name: 'photo.jpg' };
        const list = [createPendingEntry({ clientMessageId: 'cid-1', callerUserAccountId: 'caller-1', kind: 2, file, durationSeconds: 0, nowIso: '2026-07-16T01:00:00.000Z' })];
        setPendingMediaId(list, 'pending-cid-1', 'media-9');
        expect(list[0].retryPayload.mediaId).toBeNull();
    });

    it('returns the same reference when the id is absent', () => {
        const list = [makeTextPending('cid-1')];
        expect(setPendingMediaId(list, 'pending-missing', 'media-9')).toBe(list);
    });
});

describe('removePendingById', () => {
    it('removes the matching entry', () => {
        const list = [makeTextPending('cid-1'), makeTextPending('cid-2')];
        const next = removePendingById(list, 'pending-cid-1');
        expect(next).toHaveLength(1);
        expect(next[0].id).toBe('pending-cid-2');
    });

    it('returns the same reference when the id is absent', () => {
        const list = [makeTextPending('cid-1')];
        expect(removePendingById(list, 'pending-missing')).toBe(list);
    });
});

describe('reconcilePending', () => {
    it('drops a pending entry once a server entry echoes its clientMessageId', () => {
        const list = [makeTextPending('cid-1'), makeTextPending('cid-2')];
        const next = reconcilePending(list, [makeServerEntry('m1', 5, { clientMessageId: 'cid-1' })]);
        expect(next).toHaveLength(1);
        expect(next[0].clientMessageId).toBe('cid-2');
    });

    it('drops a failed entry the same way, healing the lost-response ghost', () => {
        const list = [makeTextPending('cid-1', { failed: true })];
        const next = reconcilePending(list, [makeServerEntry('m1', 5, { clientMessageId: 'cid-1' })]);
        expect(next).toHaveLength(0);
    });

    it('drops multiple matches in one pass', () => {
        const list = [makeTextPending('cid-1'), makeTextPending('cid-2'), makeTextPending('cid-3')];
        const next = reconcilePending(list, [
            makeServerEntry('m1', 5, { clientMessageId: 'cid-1' }),
            makeServerEntry('m2', 6, { clientMessageId: 'cid-3' }),
        ]);
        expect(next).toHaveLength(1);
        expect(next[0].clientMessageId).toBe('cid-2');
    });

    it('keeps pendings when server entries carry no clientMessageId', () => {
        const list = [makeTextPending('cid-1')];
        expect(reconcilePending(list, [makeServerEntry('m1', 5)])).toBe(list);
    });

    it('returns the same reference when nothing matches', () => {
        const list = [makeTextPending('cid-1')];
        expect(reconcilePending(list, [makeServerEntry('m1', 5, { clientMessageId: 'cid-other' })])).toBe(list);
    });

    it('returns the same reference for empty inputs', () => {
        const list = [makeTextPending('cid-1')];
        expect(reconcilePending(list, [])).toBe(list);
        expect(reconcilePending(list, null)).toBe(list);
        const empty = [];
        expect(reconcilePending(empty, [makeServerEntry('m1', 5, { clientMessageId: 'cid-1' })])).toBe(empty);
    });
});

describe('orderMessages', () => {
    it('sorts confirmed messages by sequence and appends pendings in creation order', () => {
        const byId = upsertEntries({}, [makeServerEntry('m2', 2), makeServerEntry('m1', 1)]);
        const pending = [makeTextPending('cid-1'), makeTextPending('cid-2')];
        const ordered = orderMessages(byId, pending);
        expect(ordered.map((entry) => entry.id)).toEqual(['m1', 'm2', 'pending-cid-1', 'pending-cid-2']);
    });

    it('sorts sequences numerically, not lexically', () => {
        const byId = upsertEntries({}, [makeServerEntry('m10', 10), makeServerEntry('m2', 2)]);
        const ordered = orderMessages(byId, []);
        expect(ordered.map((entry) => entry.sequence)).toEqual([2, 10]);
    });

    it('keeps deleted messages in the timeline', () => {
        const byId = upsertEntries({}, [makeServerEntry('m1', 1, { isDeleted: true })]);
        expect(orderMessages(byId, [])).toHaveLength(1);
    });
});

describe('buildReplyContext', () => {
    it('builds a full context from a text parent with a known sender', () => {
        const parent = makeServerEntry('m1', 1, { body: 'help me think this through' });
        const context = buildReplyContext(parent, { 'caller-1': { id: 'caller-1', displayName: 'Sam' } });
        expect(context).toEqual({ messageId: 'm1', senderUserAccountId: 'caller-1', senderDisplayName: 'Sam', kind: 1, preview: 'help me think this through', isDeleted: false });
    });

    it('truncates long parent bodies to 140 characters', () => {
        const parent = makeServerEntry('m1', 1, { body: 'a'.repeat(300) });
        const context = buildReplyContext(parent, {});
        expect(context.preview).toBe('a'.repeat(140));
    });

    it('gives media parents a null preview while keeping the kind', () => {
        const parent = makeServerEntry('m1', 1, { kind: 2, body: null, mediaUrl: 'https://media/m1' });
        const context = buildReplyContext(parent, {});
        expect(context.kind).toBe(2);
        expect(context.preview).toBeNull();
    });

    it('marks deleted parents with a null preview', () => {
        const parent = makeServerEntry('m1', 1, { isDeleted: true, body: null });
        const context = buildReplyContext(parent, {});
        expect(context.isDeleted).toBe(true);
        expect(context.preview).toBeNull();
    });

    it('tolerates deleted-account parents, unknown senders, and null parents', () => {
        const orphanParent = makeServerEntry('m1', 1, { senderUserAccountId: null });
        expect(buildReplyContext(orphanParent, {}).senderUserAccountId).toBeNull();
        const unknownSenderParent = makeServerEntry('m2', 2);
        expect(buildReplyContext(unknownSenderParent, {}).senderDisplayName).toBeNull();
        expect(buildReplyContext(null, {})).toBeNull();
    });
});

describe('resolveReplyDisplay', () => {
    const embedded = { messageId: 'm1', senderUserAccountId: 'caller-1', senderDisplayName: 'Sam', kind: 1, preview: 'the original', isDeleted: false };

    it('prefers the live parent so later deletions tombstone the chip', () => {
        const byId = upsertEntries({}, [makeServerEntry('m1', 1, { isDeleted: true, body: null })]);
        const display = resolveReplyDisplay(embedded, byId, {});
        expect(display.isDeleted).toBe(true);
        expect(display.preview).toBeNull();
        expect(display.parentIsLoaded).toBe(true);
    });

    it('reflects the live parent body when it is loaded', () => {
        const byId = upsertEntries({}, [makeServerEntry('m1', 1, { body: 'live body' })]);
        const display = resolveReplyDisplay(embedded, byId, { 'caller-1': { id: 'caller-1', displayName: 'Sam' } });
        expect(display.preview).toBe('live body');
        expect(display.senderDisplayName).toBe('Sam');
        expect(display.parentIsLoaded).toBe(true);
    });

    it('falls back to the embedded context when the parent is beyond loaded pages', () => {
        const display = resolveReplyDisplay(embedded, {}, {});
        expect(display).toEqual({ messageId: 'm1', senderDisplayName: 'Sam', kind: 1, preview: 'the original', isDeleted: false, parentIsLoaded: false });
    });

    it('keeps the embedded sender name when the live sender is unknown', () => {
        const byId = upsertEntries({}, [makeServerEntry('m1', 1, { body: 'live body' })]);
        const display = resolveReplyDisplay(embedded, byId, {});
        expect(display.senderDisplayName).toBe('Sam');
    });

    it('returns null for entries that are not replies', () => {
        expect(resolveReplyDisplay(null, {}, {})).toBeNull();
        expect(resolveReplyDisplay(undefined, {}, {})).toBeNull();
    });
});

describe('full lifecycle scenarios', () => {
    it('poll delivers the committed message before the send response, then reconcile removes the local copy', () => {
        let pending = [makeTextPending('cid-1')];
        let byId = {};
        byId = upsertEntries(byId, [makeServerEntry('m1', 7, { clientMessageId: 'cid-1', body: 'hello' })]);
        pending = reconcilePending(pending, [makeServerEntry('m1', 7, { clientMessageId: 'cid-1', body: 'hello' })]);
        const ordered = orderMessages(byId, pending);
        expect(ordered).toHaveLength(1);
        expect(ordered[0].id).toBe('m1');
    });

    it('failure then retry then duplicate response converges to a single confirmed message', () => {
        let pending = [makeTextPending('cid-1')];
        pending = markPendingFailed(pending, 'pending-cid-1');
        expect(pending[0].failed).toBe(true);
        pending = markPendingRetrying(pending, 'pending-cid-1');
        expect(pending[0].failed).toBe(false);
        const duplicateMessage = makeServerEntry('m1', 3, { clientMessageId: 'cid-1' });
        let byId = upsertEntries({}, [duplicateMessage]);
        pending = removePendingById(pending, 'pending-cid-1');
        const ordered = orderMessages(byId, pending);
        expect(ordered).toHaveLength(1);
        expect(ordered[0].sequence).toBe(3);
    });

    it('media upload succeeds but send fails, retry keeps the captured mediaId', () => {
        const file = { uri: 'file:///photo.jpg', type: 'image/jpeg', name: 'photo.jpg' };
        let pending = [createPendingEntry({ clientMessageId: 'cid-1', callerUserAccountId: 'caller-1', kind: 2, file, durationSeconds: 0, nowIso: '2026-07-16T01:00:00.000Z' })];
        pending = setPendingMediaId(pending, 'pending-cid-1', 'media-9');
        pending = markPendingFailed(pending, 'pending-cid-1');
        pending = markPendingRetrying(pending, 'pending-cid-1');
        expect(pending[0].retryPayload.mediaId).toBe('media-9');
        expect(pending[0].failed).toBe(false);
    });

    it('two rapid sends where the first fails keeps ordering stable and flags only the first', () => {
        let pending = [makeTextPending('cid-1'), makeTextPending('cid-2')];
        pending = markPendingFailed(pending, 'pending-cid-1');
        const ordered = orderMessages({}, pending);
        expect(ordered.map((entry) => entry.failed)).toEqual([true, false]);
        expect(ordered.map((entry) => entry.clientMessageId)).toEqual(['cid-1', 'cid-2']);
    });

    it('deleting a failed message removes it while other pendings survive', () => {
        let pending = [makeTextPending('cid-1', { failed: true }), makeTextPending('cid-2')];
        pending = removePendingById(pending, 'pending-cid-1');
        expect(pending).toHaveLength(1);
        expect(pending[0].clientMessageId).toBe('cid-2');
    });
});