import baseService from './baseService';

export default profileService = {
    getMyProfile: function(authToken) {
        return baseService.postJson("profile/getMyProfile", { AuthToken: authToken });
    },
    getPublicUserProfile: function(authToken, username) {
        return baseService.postJson("profile/getPublicUserProfile", { AuthToken: authToken, Username: username });
    }
};