import http from "k6/http";
import { check, sleep } from "k6";
// Script de carga general K6
export const options = {
  stages: [
    { duration: "30s", target: 10 },
    { duration: "1m", target: 50 },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<2000"],
    http_req_failed: ["rate<0.1"],
  },
};

const BASE_URL = "http://localhost:5070/api";

export default function () {
  const categorias = http.get(`${BASE_URL}/Categorias`);
  check(categorias, { "categorias 200": (r) => r.status === 200 });

  const productos = http.get(`${BASE_URL}/Productos`);
  check(productos, { "productos 200": (r) => r.status === 200 });

  const clientes = http.get(`${BASE_URL}/Clientes`);
  check(clientes, { "clientes 200": (r) => r.status === 200 });

  const proveedores = http.get(`${BASE_URL}/Proveedores`);
  check(proveedores, { "proveedores 200": (r) => r.status === 200 });

  sleep(1);
}
