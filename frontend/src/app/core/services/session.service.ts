import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { BookSessionRequest, MySessionsResponse, SessionResponse } from '../models/session.models';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/sessions';

  // /book, not POST /api/sessions: only this slice enforces availability, R2 and R4.
  bookSession(request: BookSessionRequest): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(`${this.baseUrl}/book`, request);
  }

  getBookedSessionsForTutor(tutorId: string): Observable<SessionResponse[]> {
    return this.http
      .get<MySessionsResponse>(`${this.baseUrl}/my`, {
        params: { userId: tutorId, role: 'Tutor', status: 'Booked' }
      })
      .pipe(map(response => response.sessions));
  }
}
