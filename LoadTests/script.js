import http from 'k6/http';
import exec from 'k6/execution';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL;

export let options = {
    stages: [
        { duration: '30s', target: 50 }, // Ramp-up to 50 users over 30 seconds
        { duration: '30s', target: 0 },  // Ramp-down to 0 users over 30 seconds
    ],
    thresholds: {
        http_req_duration: ['p(95)<5000'], // 95% of requests should be below 5s
    },
    cloud: {
        // Project: Default project
        projectID: 3706889,
        // Test runs with the same name groups test runs together.
        name: 'DemoTest'
      }
};

export default function () {
  /*console.log(`Execution context

    Instance info
    -------------
    Vus active: ${exec.instance.vusActive}
    Iterations completed: ${exec.instance.iterationsCompleted}
    Iterations interrupted:  ${exec.instance.iterationsInterrupted}
    Iterations completed:  ${exec.instance.iterationsCompleted}
    Iterations active:  ${exec.instance.vusActive}
    Initialized vus:  ${exec.instance.vusInitialized}
    Time passed from start of run(ms):  ${exec.instance.currentTestRunDuration}
    
    Test info
    ---------
    All test options: ${exec.test.options}
    
    VU info
    -------
    Iteration id: ${exec.vu.iterationInInstance}
    Iteration in scenario: ${exec.vu.iterationInScenario}
    VU ID in instance: ${exec.vu.idInInstance}
    VU ID in test: ${exec.vu.idInTest}
    VU tags: ${exec.vu.tags}`);*/

    let res = http.get(`http://${baseUrl}/WeatherForecast`);
    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time is less than 5s': (r) => r.timings.duration < 5000,
    });
    sleep(1); // Wait for 1 second between iterations
}
