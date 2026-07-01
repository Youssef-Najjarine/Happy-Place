let pendingChatGroupId = null;
let handled = false;

const pendingInvite = {
    set: function(chatGroupId) {
        if (chatGroupId) pendingChatGroupId = chatGroupId;
    },
    peek: function() {
        return pendingChatGroupId;
    },
    markHandled: function() {
        handled = true;
        pendingChatGroupId = null;
    },
    wasHandled: function() {
        return handled;
    },
    clear: function() {
        pendingChatGroupId = null;
    }
};

export default pendingInvite;