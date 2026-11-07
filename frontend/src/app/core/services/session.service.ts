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

  bookSession(request: BookSessionRequest): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(this.baseUrl, request);
  }

  getBookedSessionsForTutor(tutorId: string): Observable<SessionResponse[]> {
    return this.http
      .get<MySessionsResponse>(`${this.baseUrl}/my`, {
        params: { userId: tutorId, role: 'Tutor', status: 'Booked' }
      })
      .pipe(map(response => response.sessions));
  }
}
