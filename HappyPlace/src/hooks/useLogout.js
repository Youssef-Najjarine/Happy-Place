import { useNavigation } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { clearUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import pushNotificationService from 'services/pushNotificationService';
import helpSessionStorage from 'services/helpSessionStorage';
import { helpApi, useSetAvailabilityMutation } from 'store/helpApi';
import { chatGroupsApi } from 'store/chatGroupsApi';
import { friendsApi } from 'store/friendsApi';

export default function useLogout() {
    const navigation = useNavigation();
    const dispatch = useDispatch();
    const [setAvailability] = useSetAvailabilityMutation();

    const logout = async () => {
        const token = await tokenStorage.getToken();
        if (token) {
            try {
                await setAvailability({ authToken: token, isAvailable: false }).unwrap();
            } catch {}
        }
        await pushNotificationService.unregisterDevice(token);
        await tokenStorage.clearToken();
        await helpSessionStorage.clear();
        dispatch(clearUser());
        dispatch(helpApi.util.resetApiState());
        dispatch(chatGroupsApi.util.resetApiState());
        dispatch(friendsApi.util.resetApiState());
        navigation.reset({ index: 0, routes: [{ name: 'Home' }] });
    };

    return logout;
}