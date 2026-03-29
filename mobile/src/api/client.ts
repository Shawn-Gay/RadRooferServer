import { useAuthStore } from '../store/auth';
import type {
  LoginResponse,
  AppointmentStats,
  Appointment,
  CallLog,
  Location,
  PagedResult,
} from '../types/api';

const BASE_URL = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5000';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = useAuthStore.getState().token;

  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({ error: 'Request failed' }));
    throw new Error(body.error ?? `HTTP ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export const api = {
  login: (email: string, password: string) =>
    request<LoginResponse>('/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  getAppointmentStats: (locationId?: string | null) =>
    request<AppointmentStats>(
      `/v1/appointments/stats${locationId ? `?locationId=${locationId}` : ''}`
    ),

  getAppointments: (locationId?: string | null, page = 1) =>
    request<PagedResult<Appointment>>(
      `/v1/appointments?page=${page}&pageSize=25${locationId ? `&locationId=${locationId}` : ''}`
    ),

  getLocations: () =>
    request<{ items: Location[] }>('/v1/locations'),

  toggleAssistant: (locationId: string, enabled: boolean) =>
    request<{ id: string; vapiEnabled: boolean }>(`/v1/locations/${locationId}/assistant`, {
      method: 'PUT',
      body: JSON.stringify({ enabled }),
    }),

  getCallLogs: (locationId?: string | null, page = 1) =>
    request<PagedResult<CallLog>>(
      `/v1/call-logs?page=${page}&pageSize=25${locationId ? `&locationId=${locationId}` : ''}`
    ),

  updateIntegrations: (locationId: string, calendarId: string) =>
    request<{ id: string; calendarId: string | null }>(`/v1/locations/${locationId}/integrations`, {
      method: 'PUT',
      body: JSON.stringify({ calendarId }),
    }),
};
