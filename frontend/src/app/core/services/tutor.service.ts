import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateTutorRequest,
  CreateTutorResponse,
  UpdateTutorRequest,
  UpdateAvailabilityRequest,
  TutorDetailsResponse
} from '../models/tutor.models';
import { RateSessionRequest, TutorSearchFilters, TutorSearchResponse } from '../models/tutor-search.models';

@Injectable({
  providedIn: 'root'
})
export class TutorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/tutors';

  createTutor(request: CreateTutorRequest): Observable<CreateTutorResponse> {
    return this.http.post<CreateTutorResponse>(this.baseUrl, request);
  }

  getTutorById(id: string): Observable<TutorDetailsResponse> {
    return this.http.get<TutorDetailsResponse>(`${this.baseUrl}/${id}`);
  }

  updateTutor(id: string, request: UpdateTutorRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  updateAvailability(id: string, request: UpdateAvailabilityRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/availability`, request);
  }

  searchTutors(filters: TutorSearchFilters): Observable<TutorSearchResponse> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize)
      .set('sortBy', filters.sortBy);

    const subject = filters.subject.trim();
    if (subject) {
      params = params.set('subject', subject);
    }
    if (filters.maxPrice !== null) {
      params = params.set('maxPrice', filters.maxPrice);
    }
    if (filters.date) {
      params = params.set('date', filters.date);
    }

    return this.http.get<TutorSearchResponse>(`${this.baseUrl}/search`, { params });
  }

  rateSession(
    tutorId: string,
    sessionId: string,
    studentId: string,
    request: RateSessionRequest
  ): Observable<void> {
    const headers = new HttpHeaders({ 'X-Student-Id': studentId });
    return this.http.post<void>(`${this.baseUrl}/${tutorId}/sessions/${sessionId}/rate`, request, { headers });
  }
}
