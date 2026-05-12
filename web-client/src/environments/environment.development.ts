import { AppEnvironment } from "./environment.model";

export const environment: AppEnvironment = {
    production: false,
    productServiceUrl: 'http://localhost:5001',
    stockServiceUrl: 'http://localhost:5002',
    paymentServiceUrl: 'http://localhost:5003'
};