import { createApi } from '@reduxjs/toolkit/query/react';
import baseService from 'src/services/baseService';

async function baseQuery(args) {
    try {
        const response = await baseService.postJson(args.path, args.body);
        if (!response.ok) {
            const errorBody = await response.text();
            return { error: { status: response.status, data: errorBody } };
        }
        const text = await response.text();
        const data = text ? JSON.parse(text) : null;
        return { data };
    } catch (error) {
        return { error: { status: 'FETCH_ERROR', error: String(error && error.message ? error.message : error) } };
    }
}

function patchAllFeeds(dispatch, getState, mutate) {
    const queries = getState()[chatGroupsApi.reducerPath].queries;
    const patches = [];
    for (const cacheKey in queries) {
        const entry = queries[cacheKey];
        if (!entry || entry.status !== 'fulfilled') continue;
        if (entry.endpointName === 'listChatGroups') {
            patches.push(dispatch(chatGroupsApi.util.updateQueryData('listChatGroups', entry.originalArgs, mutate)));
        } else if (entry.endpointName === 'listChatGroupsPage') {
            patches.push(dispatch(chatGroupsApi.util.updateQueryData('listChatGroupsPage', entry.originalArgs, (draft) => mutate(draft.items))));
        }
    }
    return patches;
}

async function runOptimistic(patches, queryFulfilled) {
    try {
        await queryFulfilled;
    } catch {
        patches.forEach((patch) => patch.undo());
    }
}

export const chatGroupsApi = createApi({
    reducerPath: 'chatGroupsApi',
    baseQuery,
    tagTypes: ['ChatGroupList', 'AvailableHelpers', 'ChatGroupMembers'],
    endpoints: (builder) => ({
        listChatGroups: builder.query({
            query: (args) => ({ path: 'chatGroup/list', body: { AuthToken: args.authToken, SortBy: args.sortBy, Search: args.search } }),
            transformResponse: (response) => (response || []).map((chatGroup) => ({ ...chatGroup, public: chatGroup.isPublic })),
            providesTags: ['ChatGroupList'],
        }),
        listChatGroupsPage: builder.query({
            query: (args) => ({ path: 'chatGroup/listPage', body: { AuthToken: args.authToken, SortBy: args.sortBy, Search: args.search, Cursor: args.cursor } }),
            transformResponse: (page) => ({
                items: (page.items || []).map((chatGroup) => ({ ...chatGroup, public: chatGroup.isPublic })),
                nextCursor: page.nextCursor == null ? null : page.nextCursor
            }),
            serializeQueryArgs: ({ queryArgs }) => ({
                authToken: queryArgs.authToken,
                sortBy: queryArgs.sortBy,
                search: queryArgs.search
            }),
            merge: (currentCache, newPage, { arg }) => {
                if (!arg.cursor) {
                    const cacheHasDeeperPages = currentCache.items.length > newPage.items.length;
                    if (newPage.nextCursor === null || !cacheHasDeeperPages) {
                        currentCache.items = newPage.items;
                        currentCache.nextCursor = newPage.nextCursor;
                        return;
                    }
                    const freshIds = new Set(newPage.items.map((item) => item.id));
                    const survivingTail = currentCache.items.filter((item, itemIndex) => !freshIds.has(item.id) && itemIndex >= newPage.items.length);
                    currentCache.items = [...newPage.items, ...survivingTail];
                    return;
                }
                const existingIds = new Set(currentCache.items.map((item) => item.id));
                currentCache.items.push(...newPage.items.filter((item) => !existingIds.has(item.id)));
                currentCache.nextCursor = newPage.nextCursor;
            },
            forceRefetch: ({ currentArg, previousArg }) => currentArg?.cursor !== previousArg?.cursor,
            providesTags: ['ChatGroupList'],
        }),
        availableHelpers: builder.query({
            query: (authToken) => ({ path: 'chatGroup/availableHelpers', body: { AuthToken: authToken } }),
            providesTags: ['AvailableHelpers'],
        }),
        listMembers: builder.query({
            query: (args) => ({ path: 'chatGroup/listMembers', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
            providesTags: ['ChatGroupMembers'],
        }),
        renameChatGroup: builder.mutation({
            query: (args) => ({ path: 'chatGroup/rename', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, Name: args.name } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    const group = draft.find((entry) => entry.id === args.chatGroupId);
                    if (group) group.title = args.name;
                });
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        setChatGroupVisibility: builder.mutation({
            query: (args) => ({ path: 'chatGroup/setVisibility', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, IsPublic: args.isPublic } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    const group = draft.find((entry) => entry.id === args.chatGroupId);
                    if (group) {
                        group.isPublic = args.isPublic;
                        group.public = args.isPublic;
                    }
                });
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        deleteChatGroup: builder.mutation({
            query: (args) => ({ path: 'chatGroup/delete', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    const index = draft.findIndex((entry) => entry.id === args.chatGroupId);
                    if (index !== -1) draft.splice(index, 1);
                });
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        leaveChatGroup: builder.mutation({
            query: (args) => {
                const body = { AuthToken: args.authToken, ChatGroupId: args.chatGroupId };
                if (args.disposition) body.Disposition = args.disposition;
                return { path: 'chatGroup/leave', body };
            },
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    if (args.disposition === 'delete') {
                        const index = draft.findIndex((entry) => entry.id === args.chatGroupId);
                        if (index !== -1) draft.splice(index, 1);
                        return;
                    }
                    const group = draft.find((entry) => entry.id === args.chatGroupId);
                    if (!group) return;
                    group.joined = false;
                    if (args.disposition === 'makePublic') {
                        group.owner = false;
                        group.isPublic = true;
                        group.public = true;
                    }
                });
                await runOptimistic(patches, queryFulfilled);
            },
            invalidatesTags: ['ChatGroupList'],
        }),
        requestToJoinChatGroup: builder.mutation({
            query: (args) => ({ path: 'chatGroup/requestToJoin', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    const group = draft.find((entry) => entry.id === args.chatGroupId);
                    if (group) group.joinRequest = true;
                });
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        cancelJoinRequest: builder.mutation({
            query: (args) => ({ path: 'chatGroup/cancelJoinRequest', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    const group = draft.find((entry) => entry.id === args.chatGroupId);
                    if (group) group.joinRequest = false;
                });
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        joinPublicChatGroup: builder.mutation({
            query: (args) => ({ path: 'helpOffer/join', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchAllFeeds(dispatch, getState, (draft) => {
                    const group = draft.find((entry) => entry.id === args.chatGroupId);
                    if (group) group.joined = true;
                });
                await runOptimistic(patches, queryFulfilled);
            },
            invalidatesTags: ['ChatGroupList'],
        }),
        approveMember: builder.mutation({
            query: (args) => ({ path: 'chatGroup/approveMember', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, MemberUserAccountId: args.memberUserAccountId } }),
            invalidatesTags: ['ChatGroupMembers', 'ChatGroupList'],
        }),
        rejectMember: builder.mutation({
            query: (args) => ({ path: 'chatGroup/rejectMember', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, MemberUserAccountId: args.memberUserAccountId } }),
            invalidatesTags: ['ChatGroupMembers', 'ChatGroupList'],
        }),
        removeMember: builder.mutation({
            query: (args) => ({ path: 'chatGroup/removeMember', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId, MemberUserAccountId: args.memberUserAccountId } }),
            invalidatesTags: ['ChatGroupMembers', 'ChatGroupList'],
        }),
    }),
});

export const {
    useListChatGroupsQuery,
    useListChatGroupsPageQuery,
    useLazyListChatGroupsPageQuery,
    useAvailableHelpersQuery,
    useListMembersQuery,
    useRenameChatGroupMutation,
    useSetChatGroupVisibilityMutation,
    useDeleteChatGroupMutation,
    useLeaveChatGroupMutation,
    useRequestToJoinChatGroupMutation,
    useCancelJoinRequestMutation,
    useJoinPublicChatGroupMutation,
    useApproveMemberMutation,
    useRejectMemberMutation,
    useRemoveMemberMutation,
} = chatGroupsApi;