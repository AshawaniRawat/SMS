import axios from 'axios';
import { env } from '@/config/env';
import { session } from '@/auth/session';
import { normalizeApiError } from './apiError';
export const httpClient = axios.create({ baseURL: env.apiBaseUrl, timeout: 15000, headers: { 'Content-Type': 'application/json' } });
httpClient.interceptors.request.use(config => {
  const token = session.token();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
httpClient.interceptors.response.use(response => response, (error: unknown) => {
  const normalized = normalizeApiError(error);
  if (normalized.status === 401) session.clear?.();
  // TODO: Handle 401 with a single-flight refresh-token strategy.
  return Promise.reject(error);
});
