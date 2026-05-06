import { useNavigation } from '@react-navigation/native';
import tokenStorage from 'services/tokenStorage';

export default function useLogout() {
    const navigation = useNavigation();

    const logout = async () => {
        await tokenStorage.clearToken();
        navigation.reset({ index: 0, routes: [{ name: 'Home' }] });
    };

    return logout;
}