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

function patchQueries(dispatch, getState, endpointName, mutate) {
    const queries = getState()[friendsApi.reducerPath].queries;
    const patches = [];
    for (const cacheKey in queries) {
        const entry = queries[cacheKey];
        if (!entry || entry.status !== 'fulfilled') continue;
        if (entry.endpointName === endpointName) {
            patches.push(dispatch(friendsApi.util.updateQueryData(endpointName, entry.originalArgs, (draft) => mutate(draft, entry.originalArgs))));
        }
    }
    return patches;
}

function patchSearchStatus(dispatch, getState, username, friendshipStatus) {
    return patchQueries(dispatch, getState, 'searchUsers', (draft) => {
        const row = (draft.users || []).find((entry) => entry.username === username);
        if (row) row.friendshipStatus = friendshipStatus;
    });
}

function patchOtherFriendListStatus(dispatch, getState, username, friendshipStatus) {
    return patchQueries(dispatch, getState, 'listFriendsPage', (draft, originalArgs) => {
        if (!originalArgs || !originalArgs.username) return;
        const row = (draft.items || []).find((entry) => entry.username === username);
        if (row) row.friendshipStatus = friendshipStatus;
    });
}

async function runOptimistic(patches, queryFulfilled) {
    try {
        await queryFulfilled;
    } catch {
        patches.forEach((patch) => patch.undo());
    }
}

export const friendsApi = createApi({
    reducerPath: 'friendsApi',
    baseQuery,
    tagTypes: ['FriendList', 'IncomingRequests', 'OutgoingRequests', 'UserSearch', 'BlockedList'],
    endpoints: (builder) => ({
        listFriendsPage: builder.query({
            query: (args) => ({ path: 'friendship/listFriends', body: { AuthToken: args.authToken, Username: args.username || null, Search: args.search || null, Cursor: args.cursor || null } }),
            transformResponse: (page) => ({
                status: page.status,
                totalCount: page.totalCount,
                items: page.friends || [],
                nextCursor: page.nextCursor == null ? null : page.nextCursor
            }),
            serializeQueryArgs: ({ queryArgs }) => ({
                authToken: queryArgs.authToken,
                username: queryArgs.username || null,
                search: queryArgs.search || null
            }),
            merge: (currentCache, newPage, { arg }) => {
                if (!arg.cursor) {
                    if (newPage.nextCursor == null || currentCache.items.length <= newPage.items.length) {
                        currentCache.items = [...newPage.items];
                        currentCache.nextCursor = newPage.nextCursor;
                    } else {
                        const freshUsernames = new Set(newPage.items.map((item) => item.username));
                        const pagedTail = currentCache.items.slice(newPage.items.length).filter((item) => !freshUsernames.has(item.username));
                        currentCache.items = [...newPage.items, ...pagedTail];
                    }
                    currentCache.totalCount = newPage.totalCount;
                    currentCache.status = newPage.status;
                    return;
                }
                const existingUsernames = new Set(currentCache.items.map((item) => item.username));
                currentCache.items.push(...newPage.items.filter((item) => !existingUsernames.has(item.username)));
                currentCache.nextCursor = newPage.nextCursor;
                currentCache.totalCount = newPage.totalCount;
            },
            forceRefetch: ({ currentArg, previousArg }) => currentArg?.cursor !== previousArg?.cursor,
            providesTags: ['FriendList'],
        }),
        listIncomingRequests: builder.query({
            query: (authToken) => ({ path: 'friendship/listIncomingRequests', body: { AuthToken: authToken } }),
            providesTags: ['IncomingRequests'],
        }),
        listOutgoingRequests: builder.query({
            query: (authToken) => ({ path: 'friendship/listOutgoingRequests', body: { AuthToken: authToken } }),
            providesTags: ['OutgoingRequests'],
        }),
        searchUsers: builder.query({
            query: (args) => ({ path: 'friendship/searchUsers', body: { AuthToken: args.authToken, Query: args.query || null } }),
            providesTags: ['UserSearch'],
        }),
        listBlocked: builder.query({
            query: (authToken) => ({ path: 'friendship/listBlocked', body: { AuthToken: authToken } }),
            providesTags: ['BlockedList'],
        }),
        sendFriendRequest: builder.mutation({
            query: (args) => ({ path: 'friendship/sendRequest', body: { AuthToken: args.authToken, Username: args.username } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = [
                    ...patchSearchStatus(dispatch, getState, args.username, 'requestSent'),
                    ...patchOtherFriendListStatus(dispatch, getState, args.username, 'requestSent')
                ];
                await runOptimistic(patches, queryFulfilled);
            },
            invalidatesTags: (result) => {
                if (!result) return [];
                if (result.status === 'accepted') return ['FriendList', 'IncomingRequests', 'OutgoingRequests', 'UserSearch'];
                if (result.status === 'requested') return ['OutgoingRequests'];
                return [];
            },
        }),
        cancelFriendRequest: builder.mutation({
            query: (args) => ({ path: 'friendship/cancelRequest', body: { AuthToken: args.authToken, Username: args.username } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = [
                    ...patchSearchStatus(dispatch, getState, args.username, 'none'),
                    ...patchOtherFriendListStatus(dispatch, getState, args.username, 'none'),
                    ...patchQueries(dispatch, getState, 'listOutgoingRequests', (draft) => {
                        const index = (draft.requests || []).findIndex((entry) => entry.username === args.username);
                        if (index !== -1) draft.requests.splice(index, 1);
                    })
                ];
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        acceptFriendRequest: builder.mutation({
            query: (args) => ({ path: 'friendship/acceptRequest', body: { AuthToken: args.authToken, Username: args.username } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = [
                    ...patchSearchStatus(dispatch, getState, args.username, 'friends'),
                    ...patchOtherFriendListStatus(dispatch, getState, args.username, 'friends'),
                    ...patchQueries(dispatch, getState, 'listIncomingRequests', (draft) => {
                        const index = (draft.requests || []).findIndex((entry) => entry.username === args.username);
                        if (index !== -1) draft.requests.splice(index, 1);
                    })
                ];
                await runOptimistic(patches, queryFulfilled);
            },
            invalidatesTags: (result) => (result && result.status === 'accepted' ? ['FriendList'] : []),
        }),
        declineFriendRequest: builder.mutation({
            query: (args) => ({ path: 'friendship/declineRequest', body: { AuthToken: args.authToken, Username: args.username } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = [
                    ...patchSearchStatus(dispatch, getState, args.username, 'none'),
                    ...patchOtherFriendListStatus(dispatch, getState, args.username, 'none'),
                    ...patchQueries(dispatch, getState, 'listIncomingRequests', (draft) => {
                        const index = (draft.requests || []).findIndex((entry) => entry.username === args.username);
                        if (index !== -1) draft.requests.splice(index, 1);
                    })
                ];
                await runOptimistic(patches, queryFulfilled);
            },
        }),
        unfriend: builder.mutation({
            query: (args) => ({ path: 'friendship/unfriend', body: { AuthToken: args.authToken, Username: args.username } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = [
                    ...patchSearchStatus(dispatch, getState, args.username, 'none'),
                    ...patchQueries(dispatch, getState, 'listFriendsPage', (draft, originalArgs) => {
                        const index = (draft.items || []).findIndex((entry) => entry.username === args.username);
                        if (index === -1) return;
                        if (originalArgs && originalArgs.username) {
                            draft.items[index].friendshipStatus = 'none';
                            return;
                        }
                        draft.items.splice(index, 1);
                        if (typeof draft.totalCount === 'number' && draft.totalCount > 0) draft.totalCount = draft.totalCount - 1;
                    })
                ];
                await runOptimistic(patches, queryFulfilled);
            },
            invalidatesTags: (result) => (result && result.status === 'unfriended' ? ['FriendList'] : []),
        }),
        blockUser: builder.mutation({
            query: (args) => ({ path: 'friendship/block', body: { AuthToken: args.authToken, Username: args.username } }),
            invalidatesTags: (result) => (result && result.status === 'blocked' ? ['FriendList', 'IncomingRequests', 'OutgoingRequests', 'UserSearch', 'BlockedList'] : []),
        }),
        unblockUser: builder.mutation({
            query: (args) => ({ path: 'friendship/unblock', body: { AuthToken: args.authToken, Username: args.username } }),
            async onQueryStarted(args, { dispatch, getState, queryFulfilled }) {
                const patches = patchQueries(dispatch, getState, 'listBlocked', (draft) => {
                    const index = (draft.blockedUsers || []).findIndex((entry) => entry.username === args.username);
                    if (index !== -1) draft.blockedUsers.splice(index, 1);
                });
                await runOptimistic(patches, queryFulfilled);
            },
            invalidatesTags: (result) => (result && result.status === 'unblocked' ? ['UserSearch'] : []),
        }),
    }),
});

export const {
    useListFriendsPageQuery,
    useLazyListFriendsPageQuery,
    useListIncomingRequestsQuery,
    useListOutgoingRequestsQuery,
    useSearchUsersQuery,
    useListBlockedQuery,
    useSendFriendRequestMutation,
    useCancelFriendRequestMutation,
    useAcceptFriendRequestMutation,
    useDeclineFriendRequestMutation,
    useUnfriendMutation,
    useBlockUserMutation,
    useUnblockUserMutation,
} = friendsApi;