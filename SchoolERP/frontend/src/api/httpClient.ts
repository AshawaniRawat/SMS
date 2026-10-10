import axios from 'axios';
import { env } from '@/config/env';
export const httpClient = axios.create({ baseURL: env.apiBaseUrl, timeout: 15000, headers: { 'Content-Type': 'application/json' } });
httpClient.interceptors.request.use(config => {
  // TODO: Inject JWT from the authentication provider.
  return config;
});
httpClient.interceptors.response.use(response => response, (error: unknown) => {
  // TODO: Handle 401 with a single-flight refresh-token strategy.
  return Promise.reject(error);
});