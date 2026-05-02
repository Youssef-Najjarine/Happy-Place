import { useState, useEffect } from 'react';
import tokenStorage from 'services/tokenStorage';
import authenticationService from 'services/authenticationService';

export default function useAutoSignIn(navigation) {
    const [isCheckingToken, setIsCheckingToken] = useState(true);

    useEffect(() => {
        const checkStoredToken = async () => {
            try {
                const storedToken = await tokenStorage.getToken();
                if (!storedToken) {
                    setIsCheckingToken(false);
                    return;
                }
                const response = await authenticationService.validateToken(storedToken);
                if (response.ok) {
                    navigation.reset({ index: 0, routes: [{ name: 'ChatGroups' }] });
                    return;
                }
                await tokenStorage.clearToken();
            } catch {
                await tokenStorage.clearToken();
            }
            setIsCheckingToken(false);
        };
        checkStoredToken();
    }, [navigation]);

    return isCheckingToken;
}