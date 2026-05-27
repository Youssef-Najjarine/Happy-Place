import baseService from './baseService';

export default authenticationService = {
    signUpWithEmail: function(name, email, password) {
        return baseService.postJson("userAuthentication/signUpWithEmail", { Name: name, Email: email, Password: password });
    },
    signUpWithPhone: function(name, phoneNumber, password) {
        return baseService.postJson("userAuthentication/signUpWithPhone", { Name: name, PhoneNumber: phoneNumber, Password: password });
    },
    verifyEmail: function(email, verificationCode) {
        return baseService.postJson("userAuthentication/verifyEmail", { Email: email, VerificationCode: verificationCode });
    },
    verifyPhone: function(phoneNumber, verificationCode) {
        return baseService.postJson("userAuthentication/verifyPhone", { PhoneNumber: phoneNumber, VerificationCode: verificationCode });
    },
    resendEmailCode: function(email) {
        return baseService.postJson("userAuthentication/resendEmailCode", { Email: email });
    },
    resendPhoneCode: function(phoneNumber) {
        return baseService.postJson("userAuthentication/resendPhoneCode", { PhoneNumber: phoneNumber });
    },
    signInWithEmail: function(email, password) {
        return baseService.postJson("userAuthentication/signInWithEmail", { Email: email, Password: password });
    },
    signInWithPhone: function(phoneNumber, password) {
        return baseService.postJson("userAuthentication/signInWithPhone", { PhoneNumber: phoneNumber, Password: password });
    },
    validateToken: function(authToken) {
        return baseService.postJson("userAuthentication/validateToken", { AuthToken: authToken });
    },
    forgotPasswordWithEmail: function(email) {
        return baseService.postJson("userAuthentication/forgotPasswordWithEmail", { Email: email });
    },
    forgotPasswordWithPhone: function(phoneNumber) {
        return baseService.postJson("userAuthentication/forgotPasswordWithPhone", { PhoneNumber: phoneNumber });
    },
    verifyForgotPasswordEmail: function(email, verificationCode) {
        return baseService.postJson("userAuthentication/verifyForgotPasswordEmail", { Email: email, VerificationCode: verificationCode });
    },
    verifyForgotPasswordPhone: function(phoneNumber, verificationCode) {
        return baseService.postJson("userAuthentication/verifyForgotPasswordPhone", { PhoneNumber: phoneNumber, VerificationCode: verificationCode });
    },
    resetPassword: function(resetToken, newPassword) {
        return baseService.postJson("userAuthentication/resetPassword", { ResetToken: resetToken, NewPassword: newPassword });
    }
};