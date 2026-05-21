import baseService from './baseService';

export default profileService = {
    getMyProfile: function(authToken) {
        return baseService.postJson("profile/getMyProfile", { AuthToken: authToken });
    },
    getPublicUserProfile: function(authToken, username) {
        return baseService.postJson("profile/getPublicUserProfile", { AuthToken: authToken, Username: username });
    },
    updateProfile: function(authToken, username, displayName, bio) {
        return baseService.postJson("profile/updateProfile", { AuthToken: authToken, Username: username, DisplayName: displayName, Bio: bio });
    },
    changePassword: function(authToken, currentPassword, newPassword) {
        return baseService.postJson("profile/changePassword", { AuthToken: authToken, CurrentPassword: currentPassword, NewPassword: newPassword });
    },
    verifyCurrentPassword: function(authToken, password) {
        return baseService.postJson("profile/verifyCurrentPassword", { AuthToken: authToken, Password: password });
    },
    checkUsernameAvailability: function(authToken, username) {
        return baseService.postJson("profile/checkUsernameAvailability", { AuthToken: authToken, Username: username });
    },
    deleteAccount: function(authToken, password) {
        return baseService.postJson("profile/deleteAccount", { AuthToken: authToken, Password: password });
    }
};