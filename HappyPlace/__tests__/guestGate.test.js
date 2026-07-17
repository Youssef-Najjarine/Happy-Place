import { shouldRedirectToFinishAccount } from '../src/utils/guestGate';

const hydratedRealAccount = { isLoggedIn: true, isAnonymous: false };
const hydratedGuestAccount = { isLoggedIn: true, isAnonymous: true };
const unhydratedDefaultState = { isLoggedIn: false, isAnonymous: false };
const clearedAfterLogout = { isLoggedIn: false, isAnonymous: false };
const inconsistentAnonymousFlag = { isLoggedIn: false, isAnonymous: true };

describe('shouldRedirectToFinishAccount', () => {
    test('hydrated real account passes into MyProfile', () => {
        expect(shouldRedirectToFinishAccount('MyProfile', hydratedRealAccount)).toBe(false);
    });

    test('hydrated real account passes into MyFriends', () => {
        expect(shouldRedirectToFinishAccount('MyFriends', hydratedRealAccount)).toBe(false);
    });

    test('hydrated guest is redirected from MyProfile', () => {
        expect(shouldRedirectToFinishAccount('MyProfile', hydratedGuestAccount)).toBe(true);
    });

    test('hydrated guest is redirected from MyFriends', () => {
        expect(shouldRedirectToFinishAccount('MyFriends', hydratedGuestAccount)).toBe(true);
    });

    test('unhydrated default state is redirected from MyProfile', () => {
        expect(shouldRedirectToFinishAccount('MyProfile', unhydratedDefaultState)).toBe(true);
    });

    test('unhydrated default state is redirected from MyFriends', () => {
        expect(shouldRedirectToFinishAccount('MyFriends', unhydratedDefaultState)).toBe(true);
    });

    test('state after logout is redirected from MyProfile', () => {
        expect(shouldRedirectToFinishAccount('MyProfile', clearedAfterLogout)).toBe(true);
    });

    test('anonymous flag wins even with isLoggedIn false', () => {
        expect(shouldRedirectToFinishAccount('MyProfile', inconsistentAnonymousFlag)).toBe(true);
    });

    test('missing user object is redirected from gated routes', () => {
        expect(shouldRedirectToFinishAccount('MyProfile', null)).toBe(true);
        expect(shouldRedirectToFinishAccount('MyFriends', undefined)).toBe(true);
    });

    test('Help tab is never gated in any state', () => {
        expect(shouldRedirectToFinishAccount('Help', hydratedRealAccount)).toBe(false);
        expect(shouldRedirectToFinishAccount('Help', hydratedGuestAccount)).toBe(false);
        expect(shouldRedirectToFinishAccount('Help', unhydratedDefaultState)).toBe(false);
        expect(shouldRedirectToFinishAccount('Help', null)).toBe(false);
    });

    test('ChatGroups tab is never gated in any state', () => {
        expect(shouldRedirectToFinishAccount('ChatGroups', hydratedRealAccount)).toBe(false);
        expect(shouldRedirectToFinishAccount('ChatGroups', hydratedGuestAccount)).toBe(false);
        expect(shouldRedirectToFinishAccount('ChatGroups', unhydratedDefaultState)).toBe(false);
        expect(shouldRedirectToFinishAccount('ChatGroups', null)).toBe(false);
    });

    test('unknown route names are never gated', () => {
        expect(shouldRedirectToFinishAccount('Profile', hydratedGuestAccount)).toBe(false);
        expect(shouldRedirectToFinishAccount('', unhydratedDefaultState)).toBe(false);
        expect(shouldRedirectToFinishAccount(undefined, null)).toBe(false);
    });
});