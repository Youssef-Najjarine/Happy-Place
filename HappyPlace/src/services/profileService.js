import baseService from './baseService';

export default profileService = {
    getMyProfile: function(authToken) {
        return baseService.postJson("userProfile/getMyProfile", { AuthToken: authToken });
    },
    getPublicUserProfile: function(authToken, username) {
        return baseService.postJson("userProfile/getPublicUserProfile", { AuthToken: authToken, Username: username });
    },
    updateProfile: function(authToken, username, displayName, bio) {
        return baseService.postJson("userProfile/updateProfile", { AuthToken: authToken, Username: username, DisplayName: displayName, Bio: bio });
    },
    changePassword: function(authToken, currentPassword, newPassword) {
        return baseService.postJson("userProfile/changePassword", { AuthToken: authToken, CurrentPassword: currentPassword, NewPassword: newPassword });
    },
    verifyCurrentPassword: function(authToken, password) {
        return baseService.postJson("userProfile/verifyCurrentPassword", { AuthToken: authToken, Password: password });
    },
    checkUsernameAvailability: function(authToken, username) {
        return baseService.postJson("userProfile/checkUsernameAvailability", { AuthToken: authToken, Username: username });
    },
    deleteAccount: function(authToken, password) {
        return baseService.postJson("userProfile/deleteAccount", { AuthToken: authToken, Password: password });
    },
    uploadProfilePhoto: function(authToken, photoUri, fileName, mimeType) {
        const formData = new FormData();
        formData.append('AuthToken', authToken);
        formData.append('Photo', { uri: photoUri, type: mimeType, name: fileName });
        return baseService.postMultipart("userProfile/uploadProfilePhoto", formData);
    },
    uploadBackgroundPhoto: function(authToken, photoUri, fileName, mimeType) {
        const formData = new FormData();
        formData.append('AuthToken', authToken);
        formData.append('Photo', { uri: photoUri, type: mimeType, name: fileName });
        return baseService.postMultipart("userProfile/uploadBackgroundPhoto", formData);
    },
    removeProfilePhoto: function(authToken) {
        return baseService.postJson("userProfile/removeProfilePhoto", { AuthToken: authToken });
    },
    removeBackgroundPhoto: function(authToken) {
        return baseService.postJson("userProfile/removeBackgroundPhoto", { AuthToken: authToken });
    },
    requestPhoneChange: function(authToken, currentPassword, phoneNumber) {
        return baseService.postJson("userProfile/requestPhoneChange", { AuthToken: authToken, CurrentPassword: currentPassword, PhoneNumber: phoneNumber });
    },
    verifyPhoneChange: function(authToken, phoneNumber, verificationCode) {
        return baseService.postJson("userProfile/verifyPhoneChange", { AuthToken: authToken, PhoneNumber: phoneNumber, VerificationCode: verificationCode });
    },
    requestEmailChange: function(authToken, currentPassword, emailAddress) {
        return baseService.postJson("userProfile/requestEmailChange", { AuthToken: authToken, CurrentPassword: currentPassword, EmailAddress: emailAddress });
    },
    verifyEmailChange: function(authToken, emailAddress, verificationCode) {
        return baseService.postJson("userProfile/verifyEmailChange", { AuthToken: authToken, EmailAddress: emailAddress, VerificationCode: verificationCode });
    }
};