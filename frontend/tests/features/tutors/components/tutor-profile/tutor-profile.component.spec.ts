import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import type { MockInstance } from 'vitest';
import { TutorProfileComponent } from '../../../../../src/app/features/tutors/components/tutor-profile/tutor-profile.component';
import { TutorService } from '../../../../../src/app/core/services/tutor.service';
import { SessionService } from '../../../../../src/app/core/services/session.service';
import { TutorDetailsResponse } from '../../../../../src/app/core/models/tutor.models';

describe('TutorProfileComponent', () => {
  let component: TutorProfileComponent;
  let fixture: ComponentFixture<TutorProfileComponent>;
  let tutorService: TutorService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorProfileComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'id' ? 'tutor-xyz' : null)
              }
            }
          }
        },
        TutorService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TutorProfileComponent);
    component = fixture.componentInstance;
    tutorService = TestBed.inject(TutorService);
  });

  it('should create the profile component', () => {
    expect(component).toBeTruthy();
  });

  it('Given_TutorIdInRoute_When_Initialized_Then_FetchesTutorDetails', () => {
    const mockTutor: TutorDetailsResponse = {
      id: 'tutor-xyz',
      name: 'Maria Dragomir',
      email: 'maria@test.ro',
      bio: 'Math and Discrete Mathematics tutor.',
      hourlyRate: 75,
      subjects: ['Discrete Math'],
      rating: 5.0,
      ratingCount: 3,
      availability: []
    };

    const getSpy = vi.spyOn(tutorService, 'getTutorById').mockReturnValue(of(mockTutor));

    fixture.detectChanges();

    expect(getSpy).toHaveBeenCalledWith('tutor-xyz');
    expect(component.tutor()).toEqual(mockTutor);
    expect(component.isLoading()).toBe(false);
  });

  describe('booking flow', () => {
    const SLOT = { start: '2099-11-06T10:00:00.000Z', end: '2099-11-06T11:00:00.000Z' };
    const TUTOR: TutorDetailsResponse = {
      id: 'tutor-xyz',
      name: 'Maria Dragomir',
      email: 'maria@test.ro',
      bio: 'Bio',
      hourlyRate: 75,
      availability: [{ start: '2099-11-06T10:00:00Z', end: '2099-11-06T12:00:00Z' }]
    };
    let getBookedSpy: MockInstance<SessionService['getBookedSessionsForTutor']>;

    const text = (): string => fixture.nativeElement.textContent;
    const query = (selector: string): HTMLButtonElement => fixture.nativeElement.querySelector(selector);

    beforeEach(() => {
      vi.spyOn(tutorService, 'getTutorById').mockReturnValue(of(TUTOR));
      getBookedSpy = vi
        .spyOn(TestBed.inject(SessionService), 'getBookedSessionsForTutor')
        .mockReturnValue(of([]));
      fixture.detectChanges();
    });

    const openModalAndRespond = (status: number): void => {
      query('.slot-button').click();
      fixture.detectChanges();
      query('.book-button').click();
      fixture.detectChanges();
      query('app-book-session-modal .btn-accent').click();
      TestBed.inject(HttpTestingController)
        .expectOne('/api/sessions')
        .flush(status === 201 ? { id: 'session-1' } : {}, { status, statusText: 'Status' });
      fixture.detectChanges();
    };

    it('Given_NoSelection_When_Rendering_Then_BookButtonIsDisabled', () => {
      expect(query('.book-button').disabled).toBe(true);
    });

    it('Given_SelectedSlot_When_ClickingBook_Then_OpensModalForThatSlot', () => {
      query('.slot-button').click();
      fixture.detectChanges();
      query('.book-button').click();
      fixture.detectChanges();

      expect(component.selectedSlot()).toEqual(SLOT);
      expect(query('app-book-session-modal')).toBeTruthy();
    });

    it('Given_409_When_Booking_Then_ShowsConflictDialogAndClosesModal', () => {
      openModalAndRespond(409);

      expect(query('app-concurrency-conflict-dialog')).toBeTruthy();
      expect(query('app-book-session-modal')).toBeNull();
      expect(text()).not.toContain('is booked');
    });

    it('Given_ConflictDialog_When_Refreshing_Then_ReloadsSlotsAndClearsSelection', () => {
      openModalAndRespond(409);
      getBookedSpy.mockClear();

      query('app-concurrency-conflict-dialog button').click();
      fixture.detectChanges();

      expect(getBookedSpy).toHaveBeenCalledWith('tutor-xyz');
      expect(component.selectedSlot()).toBeNull();
      expect(query('app-concurrency-conflict-dialog')).toBeNull();
    });

    it('Given_SuccessfulBooking_When_Rendered_Then_MovesFocusToSlotsHeading', () => {
      document.body.appendChild(fixture.nativeElement);

      openModalAndRespond(201);
      TestBed.tick();

      expect(document.activeElement?.textContent).toContain('Available Time Slots');
      fixture.nativeElement.remove();
    });

    it('Given_SuccessfulBooking_When_Confirmed_Then_ShowsSuccessAndReloadsSlots', () => {
      getBookedSpy.mockClear();

      openModalAndRespond(201);

      expect(text()).toContain('is booked');
      expect(getBookedSpy).toHaveBeenCalledWith('tutor-xyz');
      expect(query('app-book-session-modal')).toBeNull();
    });

    it('Given_SlotsRequestFails_When_Retrying_Then_LoadsSlots', () => {
      getBookedSpy.mockReturnValueOnce(throwError(() => new Error('down')));
      component.loadBookedSessions('tutor-xyz');
      fixture.detectChanges();
      expect(text()).toContain('could not load the available slots');

      fixture.nativeElement.querySelector('.availability-card button').click();
      fixture.detectChanges();

      expect(query('.slot-button')).toBeTruthy();
    });
  });

  it('Given_DemoProfile_When_Rendering_Then_ExplainsBookingIsUnavailable', () => {
    vi.spyOn(tutorService, 'getTutorById').mockReturnValue(throwError(() => new Error('not found')));

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('sample profile');
    expect(fixture.nativeElement.querySelector('app-slot-picker')).toBeNull();
  });

  it('Given_FullName_When_GettingInitials_Then_ReturnsTwoLetters', () => {
    expect(component.getInitials('Maria Dragomir')).toBe('MD');
    expect(component.getInitials('Radu')).toBe('R');
  });
});
