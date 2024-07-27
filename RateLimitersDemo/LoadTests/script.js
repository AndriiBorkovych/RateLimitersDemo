import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL;

export let options = {
    stages: [
        { duration: '30s', target: 50 }, // Ramp-up to 50 users over 30 seconds
        { duration: '30s', target: 50 },  // Stay at 50 users for 1 minute
        { duration: '30s', target: 0 },  // Ramp-down to 0 users over 30 seconds
    ],
    thresholds: {
        http_req_duration: ['p(95)<5000'], // 95% of requests should be below 5s
    },
};

export default function () {
    let res = http.get(`http://${baseUrl}/WeatherForecast`);
    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time is less than 500ms': (r) => r.timings.duration < 500,
    });
    sleep(1); // Wait for 1 second between iterations
}
