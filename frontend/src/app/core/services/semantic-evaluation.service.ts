import { Injectable, inject } from '@angular/core';
import { Observable, catchError, concatMap, forkJoin, from, map, of } from 'rxjs';
import {
  EvaluationCase,
  EvaluationCaseResult,
  EvaluationSummary
} from '../models/semantic-search.models';
import { TutorSearchItem } from '../models/tutor-search.models';
import { SemanticSearchService } from './semantic-search.service';
import { TutorService } from './tutor.service';

export const TOP_K = 3;

const roundToOneDecimal = (value: number): number => Math.round(value * 10) / 10;

const percentage = (hits: number, total: number): number =>
  total === 0 ? 0 : roundToOneDecimal((hits / total) * 100);

/** Aggregates per-case results into Top-3 accuracy figures. Errored cases are excluded from the denominator. */
export function summarizeEvaluation(results: readonly EvaluationCaseResult[]): EvaluationSummary {
  const errorCount = results.filter((result) => result.status === 'error').length;
  const evaluated = results.length - errorCount;
  const semanticHits = results.filter((result) => result.semanticHit).length;
  const keywordHits = results.filter((result) => result.status !== 'error' && result.keywordHit).length;
  const semanticAccuracy = percentage(semanticHits, evaluated);
  const keywordAccuracy = percentage(keywordHits, evaluated);

  return {
    total: results.length,
    evaluated,
    semanticHits,
    keywordHits,
    fallbackCount: results.filter((result) => result.status === 'fallback').length,
    errorCount,
    semanticAccuracy,
    keywordAccuracy,
    improvement: roundToOneDecimal(semanticAccuracy - keywordAccuracy)
  };
}

@Injectable({
  providedIn: 'root'
})
export class SemanticEvaluationService {
  private readonly semanticSearch = inject(SemanticSearchService);
  private readonly tutorService = inject(TutorService);

  /** Evaluates the cases one at a time to avoid flooding the embedding service. Emits each result as it completes. */
  evaluate(cases: readonly EvaluationCase[]): Observable<EvaluationCaseResult> {
    return from(cases).pipe(concatMap((testCase) => this.evaluateCase(testCase)));
  }

  private evaluateCase(testCase: EvaluationCase): Observable<EvaluationCaseResult> {
    return forkJoin({
      semantic: this.semanticSearch.search(testCase.query),
      keyword: this.keywordBaseline(testCase.query)
    }).pipe(
      map(({ semantic, keyword }): EvaluationCaseResult => {
        const semanticTop = semantic.tutors.slice(0, TOP_K);
        const isFallback = semantic.mode === 'keyword_fallback';
        const semanticHit = !isFallback && this.contains(semanticTop, testCase.expectedTutorId);
        const expected = semanticTop.find((tutor) => tutor.id === testCase.expectedTutorId);

        return {
          testCase,
          status: isFallback ? 'fallback' : semanticHit ? 'hit' : 'miss',
          semanticHit,
          keywordHit: this.contains(keyword.slice(0, TOP_K), testCase.expectedTutorId),
          matchScore: isFallback ? null : (expected?.similarityScore ?? null)
        };
      }),
      catchError(() =>
        of<EvaluationCaseResult>({
          testCase,
          status: 'error',
          semanticHit: false,
          keywordHit: false,
          matchScore: null
        })
      )
    );
  }

  /** The classic subject filter fed with the raw natural-language query. */
  private keywordBaseline(query: string): Observable<TutorSearchItem[]> {
    return this.tutorService
      .searchTutors({ subject: query, maxPrice: null, date: '', sortBy: 'priceAsc', page: 1, pageSize: TOP_K })
      .pipe(
        map((response) => response.tutors),
        catchError(() => of<TutorSearchItem[]>([]))
      );
  }

  private contains(tutors: readonly TutorSearchItem[], tutorId: string): boolean {
    return tutors.some((tutor) => tutor.id === tutorId);
  }
}
