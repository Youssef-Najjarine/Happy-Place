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
export const helpApi = createApi({
    reducerPath: 'helpApi',
    baseQuery,
    endpoints: (builder) => ({
        pollRequest: builder.query({
            query: (args) => ({ path: 'helpRequest/pollRequest', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        myOpenRequest: builder.query({
            query: (authToken) => ({ path: 'helpRequest/myOpenRequest', body: { AuthToken: authToken } }),
        }),
        openRequests: builder.query({
            query: (authToken) => ({ path: 'helpOffer/openRequests', body: { AuthToken: authToken } }),
        }),
        pollOffer: builder.query({
            query: (authToken) => ({ path: 'helpOffer/pollOffer', body: { AuthToken: authToken } }),
        }),
        createRequest: builder.mutation({
            query: (args) => ({ path: 'helpRequest/createRequest', body: { AuthToken: args.authToken, Topic: args.topic == null ? null : args.topic } }),
        }),
        connect: builder.mutation({
            query: (args) => ({ path: 'helpRequest/connect', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        cancelRequest: builder.mutation({
            query: (args) => ({ path: 'helpRequest/cancel', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        createOffer: builder.mutation({
            query: (args) => ({ path: 'helpOffer/createOffer', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        declineOffer: builder.mutation({
            query: (args) => ({ path: 'helpOffer/declineOffer', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        withdrawOffer: builder.mutation({
            query: (args) => ({ path: 'helpOffer/withdrawOffer', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        join: builder.mutation({
            query: (args) => ({ path: 'helpOffer/join', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        declineInvite: builder.mutation({
            query: (args) => ({ path: 'helpOffer/declineInvite', body: { AuthToken: args.authToken, ChatGroupId: args.chatGroupId } }),
        }),
        setAvailability: builder.mutation({
            query: (args) => ({ path: 'helpAvailability/setAvailability', body: { AuthToken: args.authToken, IsAvailable: args.isAvailable } }),
        }),
        getAvailability: builder.query({
            query: (authToken) => ({ path: 'helpAvailability/getAvailability', body: { AuthToken: authToken } }),
        }),
    }),
});
export const {
    usePollRequestQuery,
    useMyOpenRequestQuery,
    useLazyMyOpenRequestQuery,
    useOpenRequestsQuery,
    usePollOfferQuery,
    useLazyPollOfferQuery,
    useCreateRequestMutation,
    useConnectMutation,
    useCancelRequestMutation,
    useCreateOfferMutation,
    useDeclineOfferMutation,
    useWithdrawOfferMutation,
    useJoinMutation,
    useDeclineInviteMutation,
    useSetAvailabilityMutation,
    useGetAvailabilityQuery,
    useLazyGetAvailabilityQuery,
} = helpApi;