// shortcut: no auth yet, everyone books as the student seeded in Program.cs; replace when identity lands.
export const DEMO_STUDENT_ID = '11111111-1111-1111-1111-111111111111';

export interface BookSessionRequest {
  tutorId: string;
  startTime: string;
  endTime: string;
}

export interface SessionResponse {
  id: string;
  tutorId: string;
  studentId: string;
  startTime: string;
  endTime: string;
  status: number;
  version: number;
}

export interface MySessionsResponse {
  sessions: SessionResponse[];
}

export interface ApiError {
  code: string;
  description: string;
}
