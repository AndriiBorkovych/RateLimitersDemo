import http from "k6/http";
import { check, sleep } from "k6";
import { Rate } from "k6/metrics";

const baseUrl = "localhost:5000";
export const errorRate = new Rate("errors");

export let options = {
  stages: [
    { duration: "30s", target: 50 },
    { duration: "30s", target: 0 },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    errors: ["rate<0.95"], // error rate should be less than 50%
    http_req_duration: ["p(95)<5000"], // 95% of requests should be below 5s
  },
  cloud: {
    // for Grafana cloud
    projectID: 3706889,
    name: "DemoTest",
  },
};

export default function () {
  let res = http.get(`http://${baseUrl}/WeatherForecast`);
  check(res, {
    "status is 200": (r) => r.status === 200,
    "response time is less than 5s": (r) => r.timings.duration < 5000,
  }) || errorRate.add(1);
  sleep(1);
}
