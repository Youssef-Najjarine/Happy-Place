import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 200,
    duration: '60s',
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed: ['rate<0.01']
    }
};

const baseUrl = __ENV.BASE_URL || 'http://192.168.1.100:5094';
const authToken = __ENV.AUTH_TOKEN;

export default function () {
    const response = http.post(`${baseUrl}/api/chatGroup/listPage`, JSON.stringify({ AuthToken: authToken }), {
        headers: { 'Content-Type': 'application/json' }
    });
    check(response, {
        'status is 200': (result) => result.status === 200,
        'has items': (result) => result.json('items') !== undefined
    });
    sleep(1);
}