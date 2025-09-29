// const baseUrl = "https://192.168.1.100:7115";
const baseUrl = "http://192.168.1.100:5094";

function getUrl(relativePath) {
    return baseUrl + "/api/" + relativePath;
}
export default baseService = {
    postJson: async function(relativePath, body) {
        const url = getUrl(relativePath);
        const bodyString = typeof body === "string" ? body : JSON.stringify(body)
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: bodyString
      });
      return response;
    }
}