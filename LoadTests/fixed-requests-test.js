import http from "k6/http";
import { check } from "k6";

const baseUrl = "localhost:5000";

export let options = {
  vus: 10, // Number of virtual users
  iterations: 100, // Total number of requests
  cloud: {
    projectID: 3706889,
    name: "FixedTestDemo"
  }
};

export default function () {
  let res = http.get(`http://${baseUrl}/WeatherForecast`);
  check(res, {
    "status is 200": (r) => r.status === 200,
  });
}
