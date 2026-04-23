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
  vapiAssistantId: string | null;
  vapiPhoneNumberId: string | null;
}

export const CallStatus = {
  AssistantEnded:   'AssistantEnded',
  CustomerHungUp:   'CustomerHungUp',
  Transferred:      'Transferred',
  Voicemail:        'Voicemail',
  NoAnswer:         'NoAnswer',
  Busy:             'Busy',
  SilenceTimeout:   'SilenceTimeout',
  ConnectionFailed: 'ConnectionFailed',
  Failed:           'Failed',
} as const;
export type CallStatus = typeof CallStatus[keyof typeof CallStatus];

export const CallDirection = {
  Inbound:  'Inbound',
  Outbound: 'Outbound',
} as const;
export type CallDirection = typeof CallDirection[keyof typeof CallDirection];

export interface CallLog {
  id: string;
  direction: CallDirection;
  status: CallStatus;
  appointmentId: string | null;
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
