import { chatGroupsApi } from 'src/store/chatGroupsApi';

export const chatMessagesApi = chatGroupsApi.injectEndpoints({
    endpoints: (builder) => ({
        listMessagesPage: builder.query({
            query: (args) => {
                const body = { AuthToken: args.authToken, ChatGroupId: args.chatGroupId };
                if (args.cursor) body.Cursor = args.cursor;
                return { path: 'chatMessage/listPage', body };
            },
            serializeQueryArgs: ({ queryArgs }) => ({ authToken: queryArgs.authToken, chatGroupId: queryArgs.chatGroupId }),
            keepUnusedDataFor: 0,
        }),
        pollMessages: builder.query({
            query: (args) => ({ path: 'chatMessage/poll', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, SinceChangeSequence: args.sinceChangeSequence } }),
            serializeQueryArgs: ({ queryArgs }) => ({ authToken: queryArgs.authToken, chatGroupId: queryArgs.chatGroupId }),
            keepUnusedDataFor: 0,
        }),
        sendChatMessage: builder.mutation({
            query: (args) => {
                const body = { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, ClientMessageId: args.clientMessageId };
                if (args.mediaId) body.MediaId = args.mediaId;
                if (args.body != null) body.Body = args.body;
                if (args.replyToMessageId) body.ReplyToMessageId = args.replyToMessageId;
                return { path: 'chatMessage/send', body };
            },
        }),
        markMessagesRead: builder.mutation({
            query: (args) => ({ path: 'chatMessage/markRead', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, UpToSequence: args.upToSequence } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                try {
                    await queryFulfilled;
                } catch {
                    return;
                }
                const queries = getState()[chatGroupsApi.reducerPath].queries;
                for (const cacheKey in queries) {
                    const entry = queries[cacheKey];
                    if (!entry || entry.status !== 'fulfilled') continue;
                    if (entry.endpointName === 'listChatGroups') {
                        dispatch(chatGroupsApi.util.updateQueryData('listChatGroups', entry.originalArgs, (draft) => {
                            const group = draft.find((item) => item.id === args.chatGroupId);
                            if (group) group.unreadCount = 0;
                        }));
                    } else if (entry.endpointName === 'listChatGroupsPage') {
                        dispatch(chatGroupsApi.util.updateQueryData('listChatGroupsPage', entry.originalArgs, (draft) => {
                            const group = draft.items.find((item) => item.id === args.chatGroupId);
                            if (group) group.unreadCount = 0;
                        }));
                    }
                }
            },
        }),
        sendTypingPing: builder.mutation({
            query: (args) => ({ path: 'chatMessage/typing', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        reactToMessage: builder.mutation({
            query: (args) => ({ path: 'chatMessage/react', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, MessageId: args.messageId, Emoji: args.emoji || '' } }),
        }),
        deleteOwnMessage: builder.mutation({
            query: (args) => ({ path: 'chatMessage/deleteOwn', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, MessageId: args.messageId } }),
        }),
        reportMessage: builder.mutation({
            query: (args) => {
                const body = { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, MessageId: args.messageId };
                if (args.reason) body.Reason = args.reason;
                return { path: 'chatMessage/report', body };
            },
        }),
    }),
});

export function selectCachedChatGroup(state, chatGroupId) {
    const queries = state[chatGroupsApi.reducerPath].queries;
    for (const cacheKey in queries) {
        const entry = queries[cacheKey];
        if (!entry || entry.status !== 'fulfilled' || !entry.data) continue;
        let feedItems = null;
        if (entry.endpointName === 'listChatGroups') feedItems = entry.data;
        else if (entry.endpointName === 'listChatGroupsPage') feedItems = entry.data.items;
        if (!Array.isArray(feedItems)) continue;
        const found = feedItems.find((group) => group.id === chatGroupId);
        if (found) return found;
    }
    return null;
}

export const {
    useLazyListMessagesPageQuery,
    useLazyPollMessagesQuery,
    useSendChatMessageMutation,
    useMarkMessagesReadMutation,
    useSendTypingPingMutation,
    useReactToMessageMutation,
    useDeleteOwnMessageMutation,
    useReportMessageMutation,
} = chatMessagesApi;