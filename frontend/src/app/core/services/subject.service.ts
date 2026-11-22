import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface SubjectSuggestionResponse {
  suggestions: string[];
}

@Injectable({
  providedIn: 'root'
})
export class SubjectService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/subjects';

  suggestSubjects(query: string): Observable<string[]> {
    const trimmed = query.trim();
    if (!trimmed) {
      return new Observable(observer => {
        observer.next([]);
        observer.complete();
      });
    }

    const params = new HttpParams().set('q', trimmed);
    return this.http
      .get<SubjectSuggestionResponse>(`${this.baseUrl}/suggest`, { params })
      .pipe(map(response => response.suggestions || []));
  }
}
