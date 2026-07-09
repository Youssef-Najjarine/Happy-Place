const baseUrl = "http://192.168.1.100:5094";
const REQUEST_TIMEOUT_MS = 8000;
const UPLOAD_TIMEOUT_MS = 60000;

function getUrl(relativePath) {
    return baseUrl + "/api/" + relativePath;
}

function getMediaUrl(photoUrl) {
    if (!photoUrl) return photoUrl;
    if (photoUrl.startsWith("http://") || photoUrl.startsWith("https://")) return photoUrl;
    return baseUrl + photoUrl;
}

const baseService = {
    postJson: async function(relativePath, body) {
        const url = getUrl(relativePath);
        const bodyString = typeof body === "string" ? body : JSON.stringify(body);
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: bodyString,
                signal: controller.signal
            });
            return response;
        } finally {
            clearTimeout(timeoutId);
        }
    },
    postMultipart: async function(relativePath, formData) {
        const url = getUrl(relativePath);
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), UPLOAD_TIMEOUT_MS);
        try {
            const response = await fetch(url, {
                method: 'POST',
                body: formData,
                signal: controller.signal
            });
            return response;
        } finally {
            clearTimeout(timeoutId);
        }
    },
    getMediaUrl: getMediaUrl
};

export default baseService;