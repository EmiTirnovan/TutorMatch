import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TutorService } from '../../../src/app/core/services/tutor.service';
import { CreateTutorRequest, CreateTutorResponse, TutorDetailsResponse, UpdateAvailabilityRequest, UpdateTutorRequest } from '../../../src/app/core/models/tutor.models';

describe('TutorService', () => {
  let service: TutorService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        TutorService
      ]
    });

    service = TestBed.inject(TutorService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should send POST request to create tutor', () => {
    const mockRequest: CreateTutorRequest = {
      name: 'Emi Tirnovan',
      email: 'emi@test.ro',
      bio: 'Algorithms tutor with 3 years of experience.',
      hourlyRate: 90
    };

    const mockResponse: CreateTutorResponse = {
      id: 'tutor-123',
      name: 'Emi Tirnovan',
      email: 'emi@test.ro',
      bio: 'Algorithms tutor with 3 years of experience.',
      hourlyRate: 90
    };

    service.createTutor(mockRequest).subscribe((res) => {
      expect(res).toEqual(mockResponse);
      expect(res.id).toBe('tutor-123');
    });

    const req = httpMock.expectOne('/api/tutors');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(mockRequest);
    req.flush(mockResponse);
  });

  it('should send GET request to fetch tutor details by id', () => {
    const tutorId = 'tutor-123';
    const mockTutor: TutorDetailsResponse = {
      id: tutorId,
      name: 'Emi Tirnovan',
      email: 'emi@test.ro',
      bio: 'Expert in dynamic programming.',
      hourlyRate: 95,
      subjects: ['Algoritmi', 'Grafuri'],
      availability: [
        { start: '2026-11-05T10:00:00Z', end: '2026-11-05T12:00:00Z' }
      ]
    };

  service.getTutorById(tutorId).subscribe((res) => {
      expect(res).toEqual(mockTutor);
      expect(res.subjects?.length).toBe(2);
    });

    const req = httpMock.expectOne(`/api/tutors/${tutorId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockTutor);
  });

  it('should send PUT request to update availability', () => {
    const tutorId = 'tutor-123';
    const mockUpdate: UpdateAvailabilityRequest = {
      availability: [
        { start: '2026-11-06T14:00:00Z', end: '2026-11-06T16:00:00Z' }
      ]
    };

    service.updateAvailability(tutorId, mockUpdate).subscribe();

    const req = httpMock.expectOne(`/api/tutors/${tutorId}/availability`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(mockUpdate);
    req.flush(null);
  });

  it('should send PUT request to update tutor profile', () => {
    const tutorId = 'tutor-123';
    const mockProfile: UpdateTutorRequest = {
      name: 'Emi T.',
      bio: 'Updated bio',
      hourlyRate: 110
    };

    service.updateTutor(tutorId, mockProfile).subscribe();

    const req = httpMock.expectOne(`/api/tutors/${tutorId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(mockProfile);
    req.flush(null);
  });

  it('Given_ActiveFilters_When_SearchingTutors_Then_SendsFiltersAsQueryParams', () => {
    service
      .searchTutors({ subject: ' SQL ', maxPrice: 80, date: '2026-11-10', sortBy: 'priceDesc', page: 2, pageSize: 6 })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/tutors/search');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('subject')).toBe('SQL');
    expect(req.request.params.get('maxPrice')).toBe('80');
    expect(req.request.params.get('date')).toBe('2026-11-10');
    expect(req.request.params.get('sortBy')).toBe('priceDesc');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('6');
    req.flush({ tutors: [], totalCount: 0, page: 2, pageSize: 6, mode: 'keyword' });
  });

  it('Given_EmptyFilters_When_SearchingTutors_Then_OmitsOptionalQueryParams', () => {
    service
      .searchTutors({ subject: '', maxPrice: null, date: '', sortBy: 'priceAsc', page: 1, pageSize: 6 })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/tutors/search');
    expect(req.request.params.has('subject')).toBe(false);
    expect(req.request.params.has('maxPrice')).toBe(false);
    expect(req.request.params.has('date')).toBe(false);
    req.flush({ tutors: [], totalCount: 0, page: 1, pageSize: 6, mode: 'keyword' });
  });

  it('Given_RatingRequest_When_RatingSession_Then_PostsScoreWithStudentHeader', () => {
    service.rateSession('tutor-1', 'session-1', 'student-1', { score: 4, notes: 'Clear explanations' }).subscribe();

    const req = httpMock.expectOne('/api/tutors/tutor-1/sessions/session-1/rate');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-Student-Id')).toBe('student-1');
    expect(req.request.body).toEqual({ score: 4, notes: 'Clear explanations' });
    req.flush(null);
  });

  it('Given_AnyFilters_When_SearchingTutors_Then_AlwaysSendsPagingAndSortParams', () => {
    service.searchTutors({ subject: '', maxPrice: null, date: '', sortBy: 'priceAsc', page: 1, pageSize: 6 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/tutors/search');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('6');
    expect(req.request.params.get('sortBy')).toBe('priceAsc');
    expect(req.request.params.keys().sort()).toEqual(['page', 'pageSize', 'sortBy']);
    req.flush({ tutors: [], totalCount: 0, page: 1, pageSize: 6, mode: 'keyword' });
  });

  it('Given_WhitespaceOnlySubject_When_SearchingTutors_Then_OmitsSubjectParam', () => {
    service.searchTutors({ subject: '   ', maxPrice: null, date: '', sortBy: 'priceAsc', page: 1, pageSize: 6 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/tutors/search');
    expect(req.request.params.has('subject')).toBe(false);
    req.flush({ tutors: [], totalCount: 0, page: 1, pageSize: 6, mode: 'keyword' });
  });

  it('Given_SearchResponse_When_SearchingTutors_Then_EmitsServerPayload', () => {
    const payload = {
      tutors: [{ id: 'tutor-1', name: 'Maria Dragomir', hourlyRate: 75, averageRating: 4.5, ratingCount: 2, subjects: ['Algoritmi', 'SQL'] }],
      totalCount: 1,
      page: 1,
      pageSize: 6,
      mode: 'keyword'
    };
    let received: unknown;

    service
      .searchTutors({ subject: '', maxPrice: null, date: '', sortBy: 'priceAsc', page: 1, pageSize: 6 })
      .subscribe((response) => (received = response));
    httpMock.expectOne((r) => r.url === '/api/tutors/search').flush(payload);

    expect(received).toEqual(payload);
  });

  it('Given_ServerFailure_When_SearchingTutors_Then_PropagatesHttpError', () => {
    let status: number | undefined;

    service
      .searchTutors({ subject: '', maxPrice: null, date: '', sortBy: 'priceAsc', page: 1, pageSize: 6 })
      .subscribe({ error: (error) => (status = error.status) });
    httpMock.expectOne((r) => r.url === '/api/tutors/search').flush('boom', { status: 500, statusText: 'Server Error' });

    expect(status).toBe(500);
  });

  it('Given_RatingWithoutNotes_When_RatingSession_Then_PostsBodyWithoutNotes', () => {
    service.rateSession('tutor-1', 'session-1', 'student-1', { score: 5 }).subscribe();

    const req = httpMock.expectOne('/api/tutors/tutor-1/sessions/session-1/rate');
    expect(req.request.body).toEqual({ score: 5 });
    expect(req.request.body).not.toHaveProperty('notes');
    req.flush(null);
  });

  it('Given_DuplicateRating_When_RatingSession_Then_PropagatesConflictWithServerDetail', () => {
    let status: number | undefined;
    let detail: string | undefined;

    service.rateSession('tutor-1', 'session-1', 'student-1', { score: 3 }).subscribe({
      error: (error) => {
        status = error.status;
        detail = error.error.detail;
      }
    });
    httpMock
      .expectOne('/api/tutors/tutor-1/sessions/session-1/rate')
      .flush({ detail: 'Această sesiune a fost deja evaluată.' }, { status: 409, statusText: 'Conflict' });

    expect(status).toBe(409);
    expect(detail).toBe('Această sesiune a fost deja evaluată.');
  });
});
