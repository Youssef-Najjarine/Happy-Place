import { useNavigation } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { clearUser } from 'store/userSlice';
import tokenStorage from 'services/tokenStorage';
import pushNotificationService from 'services/pushNotificationService';

export default function useLogout() {
    const navigation = useNavigation();
    const dispatch = useDispatch();

    const logout = async () => {
        const token = await tokenStorage.getToken();
        await pushNotificationService.unregisterDevice(token);
        dispatch(clearUser());
        await tokenStorage.clearToken();
        navigation.reset({ index: 0, routes: [{ name: 'Home' }] });
    };

    return logout;
}