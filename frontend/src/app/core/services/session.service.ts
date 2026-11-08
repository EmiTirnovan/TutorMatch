import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import {
  BookSessionRequest,
  CancelSessionRequest,
  CompleteSessionRequest,
  MySessionsResponse,
  SessionResponse
} from '../models/session.models';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v2/sessions';

  bookSession(studentId: string, request: BookSessionRequest): Observable<SessionResponse> {
    const headers = new HttpHeaders({ 'X-Student-Id': studentId });
    return this.http.post<SessionResponse>(this.baseUrl, request, { headers });
  }

  getBookedSessionsForTutor(tutorId: string): Observable<SessionResponse[]> {
    return this.getMySessions(tutorId, 'Tutor', 'Booked');
  }

  getMySessions(userId: string, role: string, status?: string): Observable<SessionResponse[]> {
    let params = new HttpParams().set('userId', userId).set('role', role);
    if (status) {
      params = params.set('status', status);
    }
    return this.http
      .get<MySessionsResponse>(`${this.baseUrl}/my`, { params })
      .pipe(map(response => response.sessions));
  }

  cancelSession(id: string, requesterId: string): Observable<void> {
    const body: CancelSessionRequest = { requesterId };
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, body);
  }

  completeSession(id: string, tutorId: string): Observable<void> {
    const body: CompleteSessionRequest = { tutorId };
    return this.http.post<void>(`${this.baseUrl}/${id}/complete`, body);
  }
}
