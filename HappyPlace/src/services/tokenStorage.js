import EncryptedStorage from 'react-native-encrypted-storage';

const TOKEN_KEY = 'auth_token';

const tokenStorage = {
    saveToken: async function(authToken) {
        await EncryptedStorage.setItem(TOKEN_KEY, authToken);
    },

    getToken: async function() {
        const storedToken = await EncryptedStorage.getItem(TOKEN_KEY);
        return storedToken || null;
    },

    clearToken: async function() {
        await EncryptedStorage.removeItem(TOKEN_KEY);
    }
};

export default tokenStorage;