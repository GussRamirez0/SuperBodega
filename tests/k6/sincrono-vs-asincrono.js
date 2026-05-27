import http from "k6/http";
import { check, sleep } from "k6";
import { Trend } from "k6/metrics";
// Comparacion de tiempos sync vs async
export const options = {
  vus: 20,
  duration: "1m",
  thresholds: {
    http_req_duration: ["p(95)<3000"],
  },
};

const BASE_URL_SINCRONO = "http://localhost:5070/api";
const BASE_URL_ASINCRONO = "http://localhost:5225/api";

const tiempoSincrono = new Trend("tiempo_sincrono");
const tiempoAsincrono = new Trend("tiempo_asincrono");

export default function () {
  const sincrono = http.get(`${BASE_URL_SINCRONO}/Productos`);
  check(sincrono, { "sincrono 200": (r) => r.status === 200 });
  tiempoSincrono.add(sincrono.timings.duration);

  const asincrono = http.get(`${BASE_URL_ASINCRONO}/Catalogo`);
  check(asincrono, { "asincrono 200": (r) => r.status === 200 });
  tiempoAsincrono.add(asincrono.timings.duration);

  sleep(1);
}
