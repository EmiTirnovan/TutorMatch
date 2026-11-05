import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TutorService } from './tutor.service';
import { CreateTutorRequest, CreateTutorResponse, TutorDetailsResponse, UpdateAvailabilityRequest, UpdateTutorRequest } from '../models/tutor.models';

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
      expect(res.subjects.length).toBe(2);
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
});
