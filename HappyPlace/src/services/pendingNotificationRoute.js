let pendingRouteName = null;
let handled = false;

const pendingNotificationRoute = {
    set: function(routeName) {
        if (routeName) pendingRouteName = routeName;
    },
    peek: function() {
        return pendingRouteName;
    },
    markHandled: function() {
        handled = true;
        pendingRouteName = null;
    },
    wasHandled: function() {
        return handled;
    },
    clear: function() {
        pendingRouteName = null;
    }
};

export default pendingNotificationRoute;