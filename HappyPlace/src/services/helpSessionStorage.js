import EncryptedStorage from 'react-native-encrypted-storage';

const HELP_SESSION_KEY = 'help_session';

const helpSessionStorage = {
    saveSeeking: async function(authToken, chatGroupId) {
        try {
            await EncryptedStorage.setItem(HELP_SESSION_KEY, JSON.stringify({ mode: 'seeking', chatGroupId, ownerToken: authToken }));
        } catch {}
    },

    saveListening: async function(authToken) {
        try {
            await EncryptedStorage.setItem(HELP_SESSION_KEY, JSON.stringify({ mode: 'listening', ownerToken: authToken }));
        } catch {}
    },

    get: async function() {
        try {
            const raw = await EncryptedStorage.getItem(HELP_SESSION_KEY);
            if (!raw) return null;
            return JSON.parse(raw);
        } catch {
            return null;
        }
    },

    clear: async function() {
        try {
            await EncryptedStorage.removeItem(HELP_SESSION_KEY);
        } catch {}
    }
};

export default helpSessionStorage;