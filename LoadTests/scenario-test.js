import http from "k6/http";
import { check } from "k6";

const baseUrl = "localhost:5000";

export let options = {
  scenarios: {
    constant_rps: {
      executor: "constant-arrival-rate",
      rate: 2, // requests per second
      timeUnit: "1s",
      duration: "1m",
      preAllocatedVUs: 5, // number of pre-allocated virtual users
      maxVUs: 10, // maximum number of virtual users
    },
  },
  cloud: {
    projectID: 3706889,
    name: "Scenario",
  },
};

export default function () {
  let res = http.get(`http://${baseUrl}/WeatherForecast`);
  check(res, {
    "status is 200": (r) => r.status === 200,
  });
}
