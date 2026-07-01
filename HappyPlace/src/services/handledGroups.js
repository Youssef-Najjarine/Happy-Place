const HandledTtlMs = 300000;

const handledAt = {};

function prune() {
    const now = Date.now();
    Object.keys(handledAt).forEach((key) => {
        if (now - handledAt[key] > HandledTtlMs) {
            delete handledAt[key];
        }
    });
}

const handledGroups = {
    markHandled: function(chatGroupId) {
        if (!chatGroupId) return;
        handledAt[chatGroupId] = Date.now();
    },
    unmark: function(chatGroupId) {
        if (!chatGroupId) return;
        delete handledAt[chatGroupId];
    },
    wasHandled: function(chatGroupId) {
        if (!chatGroupId) return false;
        prune();
        const at = handledAt[chatGroupId];
        if (at == null) return false;
        return Date.now() - at <= HandledTtlMs;
    }
};

export default handledGroups;