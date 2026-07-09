import EncryptedStorage from 'react-native-encrypted-storage';
import pushNotificationService from './pushNotificationService';
import authenticationService from './authenticationService';

const TOKEN_KEY = 'auth_token';
let sessionToken = null;
let pendingGuestCreation = null;
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
        Promise.resolve(pushNotificationService.registerDevice(authToken)).catch(() => {});
    },

    getToken: async function() {
        if (sessionToken) return sessionToken;
        const storedToken = await EncryptedStorage.getItem(TOKEN_KEY);
        if (storedToken) sessionToken = storedToken;
        return storedToken || null;
    },

    ensureGuestToken: async function() {
        const existingToken = await tokenStorage.getToken();
        if (existingToken) return existingToken;
        if (!pendingGuestCreation) {
            pendingGuestCreation = (async () => {
                try {
                    const recheckedToken = await tokenStorage.getToken();
                    if (recheckedToken) return recheckedToken;
                    const response = await authenticationService.createGuest();
                    if (!response.ok) return null;
                    const data = await response.json();
                    await tokenStorage.saveToken(data.authToken);
                    return data.authToken;
                } finally {
                    pendingGuestCreation = null;
                }
            })();
        }
        return pendingGuestCreation;
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