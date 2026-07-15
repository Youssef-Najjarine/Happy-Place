import { useState, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import { setUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import authenticationService from 'services/authenticationService';
import pendingInvite from 'services/pendingInvite';
import pendingNotificationRoute from 'services/pendingNotificationRoute';
const VALIDATION_TIMEOUT_MS = 8000;
export default function useAutoSignIn(navigation) {
    const dispatch = useDispatch();
    const [isCheckingToken, setIsCheckingToken] = useState(true);
    useEffect(() => {
        let settled = false;
        let timeoutId = null;
        const finishToHome = () => {
            if (settled) return;
            settled = true;
            if (timeoutId) clearTimeout(timeoutId);
            setIsCheckingToken(false);
        };
        const checkStoredToken = async () => {
            try {
                const storedToken = await tokenStorage.getToken();
                if (!storedToken) {
                    finishToHome();
                    return;
                }
                const response = await authenticationService.validateToken(storedToken);
                if (settled) return;
                if (response.ok) {
                    const profileData = await response.json();
                    if (settled) return;
                    settled = true;
                    if (timeoutId) clearTimeout(timeoutId);
                    dispatch(setUser(profileData));
                    const hasPendingInvite = pendingInvite.peek() || pendingInvite.wasHandled();
                    const hasPendingNotificationRoute = pendingNotificationRoute.peek() || pendingNotificationRoute.wasHandled();
                    if (!hasPendingInvite && !hasPendingNotificationRoute) {
                        navigation.reset({ index: 0, routes: [{ name: 'MainTabs' }] });
                        return;
                    }
                    setIsCheckingToken(false);
                    return;
                }
                if (response.status === 401) {
                    await tokenStorage.clearToken();
                }
                finishToHome();
            } catch {
                if (settled) return;
                finishToHome();
            }
        };
        timeoutId = setTimeout(finishToHome, VALIDATION_TIMEOUT_MS);
        checkStoredToken();
        return () => {
            settled = true;
            if (timeoutId) clearTimeout(timeoutId);
        };
    }, [navigation, dispatch]);
    return isCheckingToken;
}