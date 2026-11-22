export type TutorSortOption = 'priceAsc' | 'priceDesc' | 'rating_desc';

export interface TutorFilterCriteria {
  subject: string;
  maxPrice: number | null;
  /** Minimum average rating (1-5), or null when tutors of any rating are accepted. */
  minRating: number | null;
  /** ISO date (yyyy-MM-dd) or empty string when no date filter is applied. */
  date: string;
  sortBy: TutorSortOption;
}

export interface TutorSearchFilters extends TutorFilterCriteria {
  page: number;
  pageSize: number;
}

export const DEFAULT_FILTER_CRITERIA: TutorFilterCriteria = {
  subject: '',
  maxPrice: null,
  minRating: null,
  date: '',
  sortBy: 'priceAsc'
};

export interface TutorSearchItem {
  id: string;
  name: string;
  hourlyRate: number;
  averageRating: number;
  ratingCount: number;
  subjects: string[];
  /** Semantic similarity (0..1). Only present for semantic search results. */
  similarityScore?: number | null;
}

export interface TutorSearchResponse {
  tutors: TutorSearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  mode: string;
}

export interface RateSessionRequest {
  score: number;
  notes?: string;
}
