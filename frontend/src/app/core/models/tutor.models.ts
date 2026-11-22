import { DateRangeDto } from './date-range.dto';

export interface CreateTutorRequest {
  name: string;
  email: string;
  bio: string;
  hourlyRate: number;
  subjects?: string[];
  subjectIds?: string[];
  availability?: DateRangeDto[];
}

export interface CreateTutorResponse {
  id: string;
  name: string;
  email: string;
  bio: string;
  hourlyRate: number;
}

export interface UpdateTutorRequest {
  name: string;
  bio: string;
  hourlyRate: number;
  subjects?: string[];
}

export interface UpdateAvailabilityRequest {
  availability: DateRangeDto[];
}

export interface TutorDetailsResponse {
  id: string;
  name: string;
  email: string;
  bio: string;
  hourlyRate: number;
  subjects?: string[];
  availability: DateRangeDto[];
  /** Mean of all ratings (0 when the tutor has none). */
  averageRating?: number;
  ratingsCount?: number;
  /** Number of ratings per star level; the server always sends the keys 1 to 5. */
  ratingDistribution?: Record<string, number>;
  /** Most recent reviews first. May hold fewer entries than ratingsCount. */
  reviews?: TutorReview[];
}

export interface TutorReview {
  sessionId: string;
  /** Date of the rated session. */
  date: string;
  score: number;
  comment?: string | null;
  studentName?: string | null;
}
