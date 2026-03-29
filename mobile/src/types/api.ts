export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  tenantId: string;
  tenantName: string;
}

export interface AppointmentStats {
  today: number;
  thisWeek: number;
}

export interface Appointment {
  id: string;
  serviceLocationId: string;
  status: string;
  startTime: string;
  endTime: string;
  notes: string | null;
  googleEventId: string | null;
  createdAt: string;
}

export interface Location {
  id: string;
  name: string;
  isActive: boolean;
  vapiEnabled: boolean;
  calendarId: string | null;
}

export interface CallLog {
  id: string;
  direction: string;
  status: string;
  startedAt: string;
  endedAt: string | null;
  summary: string | null;
  transcript: string | null;
  recordingUrl: string | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
