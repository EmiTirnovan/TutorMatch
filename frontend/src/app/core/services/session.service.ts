import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { BookSessionRequest, MySessionsResponse, SessionResponse } from '../models/session.models';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/sessions';

  bookSession(studentId: string, request: BookSessionRequest): Observable<SessionResponse> {
    const headers = new HttpHeaders({ 'X-Student-Id': studentId });
    return this.http.post<SessionResponse>(this.baseUrl, request, { headers });
  }

  getBookedSessionsForTutor(tutorId: string): Observable<SessionResponse[]> {
    return this.http
      .get<MySessionsResponse>(`${this.baseUrl}/my`, {
        params: { userId: tutorId, role: 'Tutor', status: 'Booked' }
      })
      .pipe(map(response => response.sessions));
  }
}
