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
    tutorService = TestBed.inject(TestBed.inject(TutorService) ? TutorService : TutorService);
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
      averageRating: 5,
      ratingsCount: 3,
      availability: []
    };

    const getSpy = vi.spyOn(tutorService, 'getTutorById').mockReturnValue(of(mockTutor));

    fixture.detectChanges();

    expect(getSpy).toHaveBeenCalledWith('tutor-xyz');
    expect(component.tutor()).toEqual(mockTutor);
    expect(component.isLoading()).toBe(false);
  });

  describe('booking flow', () => {
    const SLOT = { start: '2099-11-06T10:00:00.000Z', end: '2099-11-06T12:00:00.000Z' };
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
        .expectOne('/api/v2/sessions')
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

    it('Given_OpenModal_When_Cancelling_Then_RemovesModalAndKeepsSelection', () => {
      query('.slot-button').click();
      fixture.detectChanges();
      query('.book-button').click();
      fixture.detectChanges();

      query('app-book-session-modal .btn-secondary').click();
      fixture.detectChanges();

      expect(query('app-book-session-modal')).toBeNull();
      expect(component.selectedSlot()).toEqual(SLOT);
      expect(query('.book-button').disabled).toBe(false);
    });

    it('Given_DailyLimitReached_When_ChoosingAnotherDate_Then_RemovesModalWithoutBooking', () => {
      getBookedSpy.mockClear();
      query('.slot-button').click();
      fixture.detectChanges();
      query('.book-button').click();
      fixture.detectChanges();
      query('app-book-session-modal .btn-accent').click();
      TestBed.inject(HttpTestingController)
        .expectOne('/api/v2/sessions')
        .flush({ code: 'Session.DailyLimit', description: 'limit' }, { status: 400, statusText: 'Bad Request' });
      fixture.detectChanges();

      query('app-book-session-modal .btn-secondary').click();
      fixture.detectChanges();

      expect(query('app-book-session-modal')).toBeNull();
      expect(text()).not.toContain('is booked');
      expect(getBookedSpy).not.toHaveBeenCalled();
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

    it('Given_TutorRole_When_Rendering_Then_BookButtonIsNotRendered', () => {
      component.userContext.setRole('tutor');
      fixture.detectChanges();

      expect(query('.book-button')).toBeNull();
      expect(fixture.nativeElement.querySelector('[data-testid="tutor-schedule-notice"]')).toBeTruthy();
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

  describe('editing profile & subjects', () => {
    beforeEach(() => {
      component.tutor.set({
        id: 'tutor-xyz',
        name: 'Maria Dragomir',
        email: 'maria@test.ro',
        bio: 'Initial bio for mathematics tutor.',
        hourlyRate: 75,
        subjects: ['Discrete Math', 'Algebra'],
        availability: []
      });
      component.isLoading.set(false);
      fixture.detectChanges();
    });

    it('Given_OpenEditProfile_When_Triggered_Then_PopulatesEditFormAndOpensModal', () => {
      component.openEditProfile();

      expect(component.isEditProfileOpen()).toBe(true);
      expect(component.editName()).toBe('Maria Dragomir');
      expect(component.editBio()).toBe('Initial bio for mathematics tutor.');
      expect(component.editHourlyRate()).toBe(75);
      expect(component.editSubjects()).toEqual(['Discrete Math', 'Algebra']);
    });

    it('Given_EditingSubjects_When_AddingAndRemovingSubject_Then_UpdatesList', () => {
      component.openEditProfile();
      component.newEditSubjectInput = 'Calculus';
      component.addEditSubject();

      expect(component.editSubjects()).toEqual(['Discrete Math', 'Algebra', 'Calculus']);
      expect(component.newEditSubjectInput).toBe('');

      component.removeEditSubject(1); // remove Algebra
      expect(component.editSubjects()).toEqual(['Discrete Math', 'Calculus']);
    });

    it('Given_ValidEditForm_When_Saved_Then_CallsUpdateTutorAndUpdatesState', () => {
      component.openEditProfile();
      component.editName.set('Maria Pop');
      component.editBio.set('Updated bio with sufficient length.');
      component.editHourlyRate.set(90);
      component.editSubjects.set(['Geometrie']);

      const updateSpy = vi.spyOn(tutorService, 'updateTutor').mockReturnValue(of(undefined as unknown as void));

      component.saveProfile();

      expect(updateSpy).toHaveBeenCalledWith('tutor-xyz', {
        name: 'Maria Pop',
        bio: 'Updated bio with sufficient length.',
        hourlyRate: 90,
        subjects: ['Geometrie']
      });
      expect(component.isEditProfileOpen()).toBe(false);
      expect(component.tutor()?.name).toBe('Maria Pop');
      expect(component.tutor()?.subjects).toEqual(['Geometrie']);
    });
  });

  describe('reviews', () => {
    const BASE: TutorDetailsResponse = {
      id: 'tutor-xyz',
      name: 'Maria Dragomir',
      email: 'maria@test.ro',
      bio: 'Bio',
      hourlyRate: 75,
      availability: []
    };

    const query = (testId: string): HTMLElement | null => fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);
    const queryAll = (testId: string): HTMLElement[] => Array.from(fixture.nativeElement.querySelectorAll(`[data-testid="${testId}"]`));

    const render = (tutor: TutorDetailsResponse): void => {
      vi.spyOn(tutorService, 'getTutorById').mockReturnValue(of(tutor));
      vi.spyOn(TestBed.inject(SessionService), 'getBookedSessionsForTutor').mockReturnValue(of([]));
      fixture.detectChanges();
    };

    const rated: TutorDetailsResponse = {
      ...BASE,
      averageRating: 4.5,
      ratingsCount: 4,
      ratingDistribution: { '1': 0, '2': 0, '3': 0, '4': 2, '5': 2 },
      reviews: [
        { sessionId: 's2', date: '2026-11-05T10:00:00Z', score: 5, comment: 'Excellent', studentName: 'Ana Pop' },
        { sessionId: 's1', date: '2026-10-01T10:00:00Z', score: 4, comment: null, studentName: 'Vlad Enache' }
      ]
    };

    it('Given_TutorWithReviews_When_Rendering_Then_HeaderShowsStarsAverageAndReviewCount', () => {
      render(rated);

      const header = query('header-rating') as HTMLElement;
      expect(header.querySelector('strong')?.textContent).toBe('4.5');
      expect(header.textContent).toContain('(4 reviews)');
      expect(header.querySelectorAll('.star').length).toBe(5);
      expect(header.querySelectorAll('.star-filled').length).toBe(5);
      expect(header.getAttribute('aria-label')).toBe('4.5 out of 5, 4 reviews');
      expect(query('header-no-reviews')).toBeNull();
    });

    it('Given_TutorWithSingleReview_When_Rendering_Then_HeaderUsesSingularLabel', () => {
      render({ ...rated, averageRating: 5, ratingsCount: 1 });

      expect((query('header-rating') as HTMLElement).textContent).toContain('(1 review)');
    });

    it('Given_NewTutorWithoutReviews_When_Rendering_Then_HeaderSaysNoReviewsInsteadOfAFakeRating', () => {
      render({ ...BASE, averageRating: 0, ratingsCount: 0, reviews: [] });

      expect(query('header-rating')).toBeNull();
      expect(query('header-no-reviews')?.textContent).toContain('No reviews yet');
      expect(fixture.nativeElement.textContent).not.toContain('5.0');
    });

    it('Given_ResponseWithoutRatingFields_When_Rendering_Then_TreatsTutorAsUnrated', () => {
      render(BASE);

      expect(query('header-no-reviews')).not.toBeNull();
      expect(query('reviews-empty')).not.toBeNull();
    });

    it('Given_TutorWithReviews_When_Rendering_Then_ReviewsSectionShowsSummaryDistributionAndList', () => {
      render(rated);

      expect(query('reviews-section')?.textContent).toContain('Reviews from students');
      expect(query('reviews-average')?.textContent?.trim()).toBe('4.5');
      expect(queryAll('distribution-percent').map((el) => el.textContent?.trim())).toEqual(['50%', '50%', '0%', '0%', '0%']);
      const items = queryAll('review-item');
      expect(items.length).toBe(2);
      expect(items[0].textContent).toContain('Ana Pop');
      expect(items[0].textContent).toContain('Excellent');
      expect(items[0].textContent).toContain('Nov 5, 2026');
      expect(items[1].textContent).toContain('Vlad Enache');
      expect(items[1].textContent).toContain('4/5');
    });

    it('Given_NewTutorWithoutReviews_When_Rendering_Then_ReviewsSectionShowsEmptyState', () => {
      render({ ...BASE, averageRating: 0, ratingsCount: 0, reviews: [] });

      expect(query('reviews-section')).not.toBeNull();
      expect(query('reviews-empty')?.textContent).toContain('No reviews yet');
      expect(queryAll('review-item').length).toBe(0);
    });

    it('Given_TutorNotFound_When_Loading_Then_ShowsNotFoundState', () => {
      vi.spyOn(tutorService, 'getTutorById').mockReturnValue(throwError(() => ({ error: { detail: 'Tutor not found' } })));

      fixture.detectChanges();

      expect(component.tutor()).toBeNull();
      expect(component.errorMessage()).toBe('Tutor not found');
      expect(fixture.nativeElement.textContent).toContain('Tutor Profile Not Found');
      expect(query('reviews-section')).toBeNull();
    });

    it('Given_TutorProfile_When_Rendering_Then_ComputesRatingSignalsFromTheResponse', () => {
      render(rated);

      expect(component.ratingsCount()).toBe(4);
      expect(component.averageRating()).toBe(4.5);
      expect(component.hasReviews()).toBe(true);
      expect(component.reviewsLabel()).toBe('4 reviews');
    });
  });
  it('Given_TutorNotFound_When_Rendering_Then_SlotPickerIsNotShown', () => {
    vi.spyOn(tutorService, 'getTutorById').mockReturnValue(throwError(() => new Error('not found')));

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-slot-picker')).toBeNull();
  });

  it('Given_FullName_When_GettingInitials_Then_ReturnsTwoLetters', () => {
    expect(component.getInitials('Maria Dragomir')).toBe('MD');
    expect(component.getInitials('Radu')).toBe('R');
  });
});
