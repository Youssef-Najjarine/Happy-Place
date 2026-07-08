import EncryptedStorage from 'react-native-encrypted-storage';
import pushNotificationService from './pushNotificationService';

const TOKEN_KEY = 'auth_token';
let sessionToken = null;
const listeners = new Set();

function notifyListeners(authToken) {
    listeners.forEach((listener) => {
        try { listener(authToken); } catch {}
    });
}

const tokenStorage = {
    saveToken: async function(authToken) {
        sessionToken = authToken;
        await EncryptedStorage.setItem(TOKEN_KEY, authToken);
        notifyListeners(authToken);
        Promise.resolve(pushNotificationService.registerDevice(authToken)).catch(() => {});
    },

    setSessionToken: function(authToken) {
        sessionToken = authToken;
        notifyListeners(authToken);
    },

    getToken: async function() {
        if (sessionToken) return sessionToken;
        const storedToken = await EncryptedStorage.getItem(TOKEN_KEY);
        if (storedToken) sessionToken = storedToken;
        return storedToken || null;
    },

    clearToken: async function() {
        sessionToken = null;
        try {
            await EncryptedStorage.removeItem(TOKEN_KEY);
        } catch {}
        notifyListeners(null);
    },

    subscribe: function(listener) {
        listeners.add(listener);
        return function() {
            listeners.delete(listener);
        };
    }
};

export default tokenStorage;