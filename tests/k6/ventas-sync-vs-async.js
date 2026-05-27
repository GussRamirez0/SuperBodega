import http from "k6/http";
import { check, sleep } from "k6";
import { Trend } from "k6/metrics";
// Comparacion de ventas sincronas y asincronas
export const options = {
  vus: 10,
  duration: "30s",
  thresholds: {
    http_req_duration: ["p(95)<5000"],
  },
};

const BASE_URL = "http://localhost:5070/api";

const tiempoSincrono = new Trend("tiempo_sincrono");
const tiempoAsincrono = new Trend("tiempo_asincrono");

const ventaPayload = JSON.stringify({
  clienteId: 1,
  direccionEntrega: "Guatemala Ciudad",
  notas: "Prueba de carga",
  items: [{ productoId: 1, cantidad: 1 }]
});

const headers = { "Content-Type": "application/json" };

export default function () {
  const sincrono = http.post(`${BASE_URL}/Ventas`, ventaPayload, { headers });
  check(sincrono, { "sincrono OK": (r) => r.status === 200 || r.status === 201 || r.status === 400 });
  tiempoSincrono.add(sincrono.timings.duration);

  const asincrono = http.post(`${BASE_URL}/async/VentasAsync`, ventaPayload, { headers });
  check(asincrono, { "asincrono OK": (r) => r.status === 202 });
  tiempoAsincrono.add(asincrono.timings.duration);

  sleep(1);
}
