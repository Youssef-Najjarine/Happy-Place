import { useNavigation } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { clearUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import pushNotificationService from 'services/pushNotificationService';
import helpSessionStorage from 'services/helpSessionStorage';

export default function useLogout() {
    const navigation = useNavigation();
    const dispatch = useDispatch();

    const logout = async () => {
        const token = await tokenStorage.getToken();
        await tokenStorage.clearToken();
        await pushNotificationService.unregisterDevice(token);
        await helpSessionStorage.clear();
        dispatch(clearUser());
        navigation.reset({ index: 0, routes: [{ name: 'Home' }] });
    };

    return logout;
}