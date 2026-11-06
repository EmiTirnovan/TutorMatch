import { TutorSearchItem } from './tutor-search.models';

/** Raw item returned by GET /api/tutors/semantic-search. */
export interface SemanticSearchResultItemDto {
  tutorId: string;
  name: string;
  bio: string;
  hourlyRate: number;
  /** Cosine similarity, 0..1. The backend sends 0 for keyword-fallback results. */
  similarityScore: number;
  subjects: string[];
}

/** Raw response of GET /api/tutors/semantic-search. */
export interface SemanticSearchResponseDto {
  results: SemanticSearchResultItemDto[];
  /** `semantic` or `keyword` (the backend already fell back to classic search). */
  mode: string;
  message?: string | null;
}

/** UI-facing provenance of a result set. */
export type SearchMode = 'semantic' | 'keyword_fallback';

export interface SemanticSearchResult {
  mode: SearchMode;
  tutors: TutorSearchItem[];
  message: string | null;
}

export interface EvaluationCase {
  query: string;
  expectedTutorId: string;
  expectedTutorName: string;
  category: string;
}

/**
 * hit: semantic search returned the expected tutor in its top 3.
 * miss: semantic search ran but did not return the expected tutor in its top 3.
 * fallback: semantic search was unavailable and keyword results were served instead.
 * error: the case could not be evaluated at all.
 */
export type EvaluationCaseStatus = 'hit' | 'miss' | 'fallback' | 'error';

export interface EvaluationCaseResult {
  testCase: EvaluationCase;
  status: EvaluationCaseStatus;
  semanticHit: boolean;
  keywordHit: boolean;
  /** Similarity of the expected tutor within the semantic results, or null when absent. */
  matchScore: number | null;
}

export interface EvaluationSummary {
  total: number;
  /** Cases that produced a result (everything except `error`). */
  evaluated: number;
  semanticHits: number;
  keywordHits: number;
  fallbackCount: number;
  errorCount: number;
  /** Top-3 accuracy in percent (0..100), rounded to one decimal. */
  semanticAccuracy: number;
  keywordAccuracy: number;
  /** semanticAccuracy - keywordAccuracy, in percentage points. */
  improvement: number;
}

export type EvaluationState = 'idle' | 'loading' | 'success' | 'error';
