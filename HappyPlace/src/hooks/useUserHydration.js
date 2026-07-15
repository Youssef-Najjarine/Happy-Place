import { useEffect, useRef } from 'react';
import { useDispatch } from 'react-redux';
import { setUser, clearUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import authenticationService from 'services/authenticationService';

export default function useUserHydration() {
    const dispatch = useDispatch();
    const hydrationSeqRef = useRef(0);

    useEffect(() => {
        const unsubscribe = tokenStorage.subscribe((authToken) => {
            const seq = hydrationSeqRef.current + 1;
            hydrationSeqRef.current = seq;
            if (!authToken) {
                dispatch(clearUser());
                return;
            }
            const hydrate = async () => {
                try {
                    const response = await authenticationService.validateToken(authToken);
                    if (hydrationSeqRef.current !== seq) return;
                    if (!response.ok) {
                        dispatch(clearUser());
                        return;
                    }
                    const profileData = await response.json();
                    if (hydrationSeqRef.current !== seq) return;
                    dispatch(setUser(profileData));
                } catch {
                }
            };
            hydrate();
        });
        return unsubscribe;
    }, [dispatch]);
}