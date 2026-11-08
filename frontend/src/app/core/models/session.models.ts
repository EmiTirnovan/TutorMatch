// Seeded demo identities matching real database rows in tutormatch.db.
export const DEMO_STUDENT_ID = '23a81600-7740-49c2-9de8-ad86816f5291';
export const DEMO_TUTOR_ID = '35ee570a-c31c-4ff7-b77d-ccce19e81d2e';
export const DEMO_STUDENT_NAME = 'Alex Radu';
export const DEMO_TUTOR_NAME = 'Emi Tirnovan';

export enum SessionStatus {
  Booked = 0,
  Cancelled = 1,
  LateCancelled = 2,
  Completed = 3
}

export interface BookSessionRequest {
  tutorId: string;
  startTime: string;
  endTime: string;
}

export interface CancelSessionRequest {
  requesterId: string;
}

export interface CompleteSessionRequest {
  tutorId: string;
}

export interface SessionResponse {
  id: string;
  tutorId: string;
  studentId: string;
  startTime: string;
  endTime: string;
  status: SessionStatus | number | string;
  version: number;
}

export interface MySessionsResponse {
  sessions: SessionResponse[];
}

export interface ApiError {
  code: string;
  description: string;
}

export function isUpcoming(status: SessionStatus | number | string): boolean {
  return status === SessionStatus.Booked || status === 0 || status === 'Booked' || status === 'Upcoming';
}

export function isCompleted(status: SessionStatus | number | string): boolean {
  return status === SessionStatus.Completed || status === 3 || status === 'Completed';
}

export function isCancelled(status: SessionStatus | number | string): boolean {
  return status === SessionStatus.Cancelled || status === 1 || status === 'Cancelled';
}

export function isLateCancelled(status: SessionStatus | number | string): boolean {
  return status === SessionStatus.LateCancelled || status === 2 || status === 'LateCancelled';
}

export function getStatusLabel(status: SessionStatus | number | string): string {
  if (isUpcoming(status)) return 'Upcoming';
  if (isCompleted(status)) return 'Completed';
  if (isLateCancelled(status)) return 'Late Cancelled';
  if (isCancelled(status)) return 'Cancelled';
  return String(status);
}
