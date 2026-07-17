const GATED_TAB_ROUTES = ['MyProfile', 'MyFriends'];

export function shouldRedirectToFinishAccount(routeName, user) {
    if (!GATED_TAB_ROUTES.includes(routeName)) return false;
    if (!user) return true;
    if (user.isAnonymous) return true;
    return !user.isLoggedIn;
}