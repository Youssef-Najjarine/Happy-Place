let pendingRoute = null;
let handled = false;

const pendingNotificationRoute = {
    set: function(routeName, params = null) {
        if (routeName) pendingRoute = { name: routeName, params };
    },
    peek: function() {
        return pendingRoute;
    },
    markHandled: function() {
        handled = true;
        pendingRoute = null;
    },
    wasHandled: function() {
        return handled;
    },
    clear: function() {
        pendingRoute = null;
    }
};

export default pendingNotificationRoute;