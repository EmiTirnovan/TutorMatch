import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, TimeoutError, catchError, map, of, throwError, timeout } from 'rxjs';
import {
  SemanticSearchResponseDto,
  SemanticSearchResult,
  SemanticSearchResultItemDto
} from '../models/semantic-search.models';
import { TutorSearchItem } from '../models/tutor-search.models';
import { TutorService } from './tutor.service';

export const SEMANTIC_SEARCH_TIMEOUT_MS = 20000;
export const SEMANTIC_RESULT_LIMIT = 5;
export const KEYWORD_FALLBACK_MESSAGE =
  'Căutarea semantică este indisponibilă momentan, afișăm rezultate pe baza cuvintelor cheie';

const BACKEND_KEYWORD_MODE = 'keyword';

@Injectable({
  providedIn: 'root'
})
export class SemanticSearchService {
  private readonly http = inject(HttpClient);
  private readonly tutorService = inject(TutorService);
  private readonly url = '/api/tutors/semantic-search';

  /**
   * Runs a natural-language search. A timeout, a network failure or a 5xx response degrades to the
   * classic keyword search instead of surfacing an error. Other failures (e.g. 4xx) are rethrown.
   */
  search(query: string): Observable<SemanticSearchResult> {
    const trimmed = query.trim();
    if (!trimmed) {
      return of<SemanticSearchResult>({ mode: 'semantic', tutors: [], message: null });
    }

    const params = new HttpParams().set('q', trimmed);
    return this.http.get<SemanticSearchResponseDto>(this.url, { params }).pipe(
      timeout(SEMANTIC_SEARCH_TIMEOUT_MS),
      map((response) => this.toResult(response)),
      catchError((error: unknown) =>
        this.isDegradable(error) ? this.keywordFallback(trimmed) : throwError(() => error)
      )
    );
  }

  private toResult(response: SemanticSearchResponseDto): SemanticSearchResult {
    const isKeyword = response.mode === BACKEND_KEYWORD_MODE;
    return {
      mode: isKeyword ? 'keyword_fallback' : 'semantic',
      tutors: response.results.map((item) => this.toTutor(item, isKeyword)),
      message: response.message ?? null
    };
  }

  private toTutor(item: SemanticSearchResultItemDto, isKeyword: boolean): TutorSearchItem {
    return {
      id: item.tutorId,
      name: item.name,
      hourlyRate: item.hourlyRate,
      averageRating: 0,
      ratingCount: 0,
      subjects: item.subjects,
      similarityScore: isKeyword ? null : item.similarityScore
    };
  }

  private isDegradable(error: unknown): boolean {
    return (
      error instanceof TimeoutError ||
      (error instanceof HttpErrorResponse && (error.status === 0 || error.status >= 500))
    );
  }

  private keywordFallback(query: string): Observable<SemanticSearchResult> {
    return this.tutorService
      .searchTutors({
        subject: query,
        maxPrice: null,
        date: '',
        sortBy: 'priceAsc',
        page: 1,
        pageSize: SEMANTIC_RESULT_LIMIT
      })
      .pipe(
        map((response) => ({
          mode: 'keyword_fallback' as const,
          tutors: response.tutors.map((tutor) => ({ ...tutor, similarityScore: null })),
          message: KEYWORD_FALLBACK_MESSAGE
        }))
      );
  }
}
