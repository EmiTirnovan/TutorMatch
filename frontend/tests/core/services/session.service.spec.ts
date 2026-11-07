import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { SessionService } from '../../../src/app/core/services/session.service';
import { BookSessionRequest, SessionResponse } from '../../../src/app/core/models/session.models';

describe('SessionService', () => {
  let service: SessionService;
  let httpMock: HttpTestingController;

  const session: SessionResponse = {
    id: 'session-1',
    tutorId: 'tutor-1',
    studentId: 'student-1',
    startTime: '2026-11-06T10:00:00Z',
    endTime: '2026-11-06T11:00:00Z',
    status: 0,
    version: 1
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(SessionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('Given_BookingRequest_When_Booking_Then_PostsToSessionsWithStudentHeader', () => {
    const request: BookSessionRequest = {
      tutorId: 'tutor-1',
      startTime: session.startTime,
      endTime: session.endTime
    };
    let result: SessionResponse | undefined;

    service.bookSession('student-1', request).subscribe(response => (result = response));
    const req = httpMock.expectOne('/api/sessions');
    req.flush(session);

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.headers.get('X-Student-Id')).toBe('student-1');
    expect(result).toEqual(session);
  });

  it('Given_TutorId_When_GettingBookedSessions_Then_ReturnsUnwrappedSessions', () => {
    let result: SessionResponse[] | undefined;

    service.getBookedSessionsForTutor('tutor-1').subscribe(response => (result = response));
    const req = httpMock.expectOne(r => r.url === '/api/sessions/my');
    req.flush({ sessions: [session] });

    expect(req.request.params.get('userId')).toBe('tutor-1');
    expect(req.request.params.get('role')).toBe('Tutor');
    expect(req.request.params.get('status')).toBe('Booked');
    expect(result).toEqual([session]);
  });
});
