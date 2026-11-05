import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateTutorRequest,
  CreateTutorResponse,
  UpdateTutorRequest,
  UpdateAvailabilityRequest,
  TutorDetailsResponse
} from '../models/tutor.models';

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
}
