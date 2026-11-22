import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StudentProfileResponse {
  id: string;
  name: string;
  email: string;
}

export interface UpdateStudentProfileRequest {
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/students';

  getStudentById(id: string): Observable<StudentProfileResponse> {
    return this.http.get<StudentProfileResponse>(`${this.baseUrl}/${id}`);
  }

  updateStudentProfile(id: string, request: UpdateStudentProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }
}
