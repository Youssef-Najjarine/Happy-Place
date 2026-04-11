import baseService from "./baseService";
export default authenticationService = {
    signUp: async function(request) {
        return await baseService.postJson("authentication/signUpWithEmail", request);
            // return await baseService.getJson("authentication/signUp", request);

    }
}