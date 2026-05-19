import { useState, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import { setUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import authenticationService from 'services/authenticationService';

export default function useAutoSignIn(navigation) {
    const dispatch = useDispatch();
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
                    const profileData = await response.json();
                    dispatch(setUser(profileData));
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
    }, [navigation, dispatch]);

    return isCheckingToken;
}