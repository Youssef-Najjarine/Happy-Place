import baseService from "./baseService";

export default authenticationService = {
    signUpWithEmail: function(name, email, password) {
        return baseService.postJson("authentication/signUpWithEmail", { Name: name, Email: email, Password: password });
    },
    signUpWithPhone: function(name, phoneNumber, password) {
        return baseService.postJson("authentication/signUpWithPhone", { Name: name, PhoneNumber: phoneNumber, Password: password });
    },
    verifyEmail: function(email, verificationCode) {
        return baseService.postJson("authentication/verifyEmail", { Email: email, VerificationCode: verificationCode });
    },
    verifyPhone: function(phoneNumber, verificationCode) {
        return baseService.postJson("authentication/verifyPhone", { PhoneNumber: phoneNumber, VerificationCode: verificationCode });
    },
    resendEmailCode: function(email) {
        return baseService.postJson("authentication/resendEmailCode", { Email: email });
    },
    resendPhoneCode: function(phoneNumber) {
        return baseService.postJson("authentication/resendPhoneCode", { PhoneNumber: phoneNumber });
    }
};