import { DateRangeDto } from './date-range.dto';

export interface CreateTutorRequest {
  name: string;
  email: string;
  bio: string;
  hourlyRate: number;
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
  subjects: string[];
  availability: DateRangeDto[];
  rating?: number;
  ratingCount?: number;
}
