import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SessionCardComponent } from '../../../../../src/app/features/sessions/components/session-card/session-card.component';
import { TimeProvider } from '../../../../../src/app/core/services/time-provider.service';
import { SessionResponse, SessionStatus, SessionType } from '../../../../../src/app/core/models/session.models';

describe('SessionCardComponent', () => {
  let component: SessionCardComponent;
  let fixture: ComponentFixture<SessionCardComponent>;
  let timeProvider: {
    now: ReturnType<typeof vi.fn>;
    isFreeCancellation: ReturnType<typeof vi.fn>;
    isLateCancellation: ReturnType<typeof vi.fn>;
    isEligibleToComplete: ReturnType<typeof vi.fn>;
    hoursUntilStart: ReturnType<typeof vi.fn>;
  };

  const baseSession: SessionResponse = {
    id: 'session-1',
    tutorId: 'tutor-xyz',
    studentId: 'student-abc',
    startTime: '2026-11-10T10:00:00Z',
    endTime: '2026-11-10T11:00:00Z',
    status: SessionStatus.Booked,
    version: 1
  };

  beforeEach(async () => {
    timeProvider = {
      now: vi.fn().mockReturnValue(new Date('2026-11-09T00:00:00Z')),
      isFreeCancellation: vi.fn().mockReturnValue(true),
      isLateCancellation: vi.fn().mockReturnValue(false),
      isEligibleToComplete: vi.fn().mockReturnValue(false), // default is future session
      hoursUntilStart: vi.fn().mockReturnValue(24)
    };

    await TestBed.configureTestingModule({
      imports: [SessionCardComponent],
      providers: [{ provide: TimeProvider, useValue: timeProvider }]
    }).compileComponents();
  });

  function setup(
    sessionOverrides: Partial<SessionResponse> = {},
    role: 'student' | 'tutor' = 'student',
    currentUserId: string | null = null
  ): void {
    fixture = TestBed.createComponent(SessionCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('session', { ...baseSession, ...sessionOverrides });
    fixture.componentRef.setInput('userRole', role);
    if (currentUserId !== null) {
      fixture.componentRef.setInput('currentUserId', currentUserId);
    }
    fixture.detectChanges();
  }

  describe('Status Badges and Labels', () => {
    it('Given_FutureBookedSession_When_Rendering_Then_DisplaysUpcomingBadge', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(false);
      setup({ status: SessionStatus.Booked });

      const badge = fixture.nativeElement.querySelector('[data-testid="status-badge"]');
      expect(badge?.textContent?.trim()).toBe('Upcoming');
      expect(badge?.classList.contains('badge-primary')).toBe(true);
    });

    it('Given_OngoingSession_When_RenderingForTutor_Then_DisplaysNeedsCompletionBadge', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(true);
      setup({ status: SessionStatus.Booked }, 'tutor');

      const badge = fixture.nativeElement.querySelector('[data-testid="status-badge"]');
      expect(badge?.textContent?.trim()).toBe('Needs Completion');
      expect(badge?.classList.contains('badge-warning')).toBe(true);
    });

    it('Given_OngoingSession_When_RenderingForStudent_Then_DisplaysInProgressBadge', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(true);
      setup({ status: SessionStatus.Booked }, 'student');

      const badge = fixture.nativeElement.querySelector('[data-testid="status-badge"]');
      expect(badge?.textContent?.trim()).toBe('In Progress');
      expect(badge?.classList.contains('badge-warning')).toBe(true);
    });

    it('Given_CompletedSession_When_Rendering_Then_DisplaysCompletedBadge', () => {
      setup({ status: SessionStatus.Completed });

      const badge = fixture.nativeElement.querySelector('[data-testid="status-badge"]');
      expect(badge?.textContent?.trim()).toBe('Completed');
      expect(badge?.classList.contains('badge-success')).toBe(true);
    });

    it('Given_CancelledSession_When_Rendering_Then_DisplaysCancelledBadge', () => {
      setup({ status: SessionStatus.Cancelled });

      const badge = fixture.nativeElement.querySelector('[data-testid="status-badge"]');
      expect(badge?.textContent?.trim()).toBe('Cancelled');
      expect(badge?.classList.contains('badge-neutral')).toBe(true);
    });

    it('Given_LateCancelledSession_When_Rendering_Then_DisplaysAmberBadgeAndLateNotice', () => {
      setup({ status: SessionStatus.LateCancelled });

      const badge = fixture.nativeElement.querySelector('[data-testid="status-badge"]');
      expect(badge?.textContent?.trim()).toBe('Late Cancelled');
      expect(badge?.classList.contains('badge-warning')).toBe(true);

      const lateNote = fixture.nativeElement.querySelector('.late-cancellation-note');
      expect(lateNote).not.toBeNull();
      expect(lateNote?.textContent).toContain('less than 12h notice');
    });
  });

  describe('Participant Context Display', () => {
    it('Given_StudentRole_When_Rendering_Then_DisplaysTutorContext', () => {
      setup({}, 'student');

      const body = fixture.nativeElement.querySelector('.card-body');
      expect(body?.textContent).toContain('Tutor');
      expect(body?.textContent).toContain('tutor-xyz');
    });

    it('Given_TutorRole_When_Rendering_Then_DisplaysStudentContext', () => {
      setup({}, 'tutor');

      const body = fixture.nativeElement.querySelector('.card-body');
      expect(body?.textContent).toContain('Student');
      expect(body?.textContent).toContain('student-abc');
    });

    it('Given_StudentRoleWithTutorName_When_Rendering_Then_DisplaysTutorName', () => {
      setup({ tutorName: 'Emi Tirnovan' }, 'student');

      const body = fixture.nativeElement.querySelector('.card-body');
      expect(body?.textContent).toContain('Emi Tirnovan');
    });

    it('Given_TutorRoleWithStudentName_When_Rendering_Then_DisplaysStudentName', () => {
      setup({ studentName: 'Alex Radu' }, 'tutor');

      const body = fixture.nativeElement.querySelector('.card-body');
      expect(body?.textContent).toContain('Alex Radu');
    });
  });

  describe('Actions', () => {
    it('Given_FutureUpcomingSession_When_ClickingCancel_Then_EmitsCancelRequested', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(false);
      setup({ status: SessionStatus.Booked }, 'student');

      let emitted: SessionResponse | undefined;
      component.cancelRequested.subscribe(s => (emitted = s));

      const cancelBtn = fixture.nativeElement.querySelector('[data-testid="cancel-btn"]') as HTMLButtonElement;
      expect(cancelBtn).not.toBeNull();
      cancelBtn.click();

      expect(emitted).toEqual(expect.objectContaining({ id: 'session-1' }));
    });

    it('Given_PastOrOngoingSession_When_Rendering_Then_CancelButtonIsNotRendered', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(true);
      setup({ status: SessionStatus.Booked }, 'student');

      const cancelBtn = fixture.nativeElement.querySelector('[data-testid="cancel-btn"]');
      expect(cancelBtn).toBeNull();
    });

    it('Given_TutorRoleAndFutureSession_When_Rendering_Then_CancelButtonIsNotRendered', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(false);
      setup({ status: SessionStatus.Booked }, 'tutor');

      const cancelBtn = fixture.nativeElement.querySelector('[data-testid="cancel-btn"]');
      expect(cancelBtn).toBeNull();
    });

    it('Given_TutorRoleAndOngoingSession_When_Rendering_Then_MarkAsCompletedButtonIsEnabledAndEmitsOnComplete', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(true);
      setup({ status: SessionStatus.Booked }, 'tutor');

      let emitted: SessionResponse | undefined;
      component.completeRequested.subscribe(s => (emitted = s));

      const completeBtn = fixture.nativeElement.querySelector('[data-testid="complete-btn"]') as HTMLButtonElement;
      expect(completeBtn).not.toBeNull();
      expect(completeBtn.disabled).toBe(false);

      completeBtn.click();
      expect(emitted).toEqual(expect.objectContaining({ id: 'session-1' }));
    });

    it('Given_TutorRoleAndFutureSession_When_Rendering_Then_MarkAsCompletedButtonIsNotRendered', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(false);
      setup({ status: SessionStatus.Booked }, 'tutor');

      const completeBtn = fixture.nativeElement.querySelector('[data-testid="complete-btn"]');
      expect(completeBtn).toBeNull();
    });

    it('Given_StudentRoleAndOngoingSession_When_Rendering_Then_ShowsAwaitingCompletionBadge', () => {
      timeProvider.isEligibleToComplete.mockReturnValue(true);
      setup({ status: SessionStatus.Booked }, 'student');

      const awaitingBadge = fixture.nativeElement.querySelector('[data-testid="student-awaiting-badge"]');
      expect(awaitingBadge).not.toBeNull();
      expect(awaitingBadge?.textContent).toContain('Awaiting tutor completion');
    });

    it('Given_StudentRole_When_SessionIsUpcoming_Then_CompleteButtonIsNotRendered', () => {
      setup({ status: SessionStatus.Booked }, 'student');

      const completeBtn = fixture.nativeElement.querySelector('[data-testid="complete-btn"]');
      expect(completeBtn).toBeNull();
    });

    it('Given_CompletedSessionAndStudentRole_When_ClickingRate_Then_EmitsRateRequested', () => {
      setup({ status: SessionStatus.Completed }, 'student');

      let emitted: SessionResponse | undefined;
      component.rateRequested.subscribe(s => (emitted = s));

      const rateBtn = fixture.nativeElement.querySelector('[data-testid="rate-btn"]') as HTMLButtonElement;
      expect(rateBtn).not.toBeNull();
      rateBtn.click();

      expect(emitted).toEqual(expect.objectContaining({ id: 'session-1' }));
    });
  });

  describe('Online vs Physical', () => {
    const query = (id: string): HTMLElement | null => fixture.nativeElement.querySelector(`[data-testid="${id}"]`);

    it('Given_UpcomingOnlineSessionWithLink_When_Rendering_Then_ShowsMeetButtonOpeningNewTab', () => {
      setup({ type: SessionType.Online, meetingLink: 'https://meet.google.com/abc-defg-hij' });

      const meet = query('meet-btn') as HTMLAnchorElement;
      expect(meet.textContent?.trim()).toBe('Join on Google Meet');
      expect(meet.href).toBe('https://meet.google.com/abc-defg-hij');
      expect(meet.target).toBe('_blank');
      expect(meet.rel).toContain('noopener');
      expect(query('location-info')).toBeNull();
    });

    it('Given_TutorRole_When_RenderingOnlineSession_Then_TutorAlsoGetsMeetButton', () => {
      setup({ type: SessionType.Online, meetingLink: 'https://meet.google.com/abc-defg-hij' }, 'tutor');

      expect(query('meet-btn')).not.toBeNull();
    });

    it('Given_CancelledOrLinklessOnlineSession_When_Rendering_Then_HidesMeetButton', () => {
      setup({ type: SessionType.Online, meetingLink: 'https://meet.google.com/x', status: SessionStatus.Cancelled });
      expect(query('meet-btn')).toBeNull();

      setup({ type: SessionType.Online, meetingLink: null });
      expect(query('meet-btn')).toBeNull();
    });

    it('Given_PhysicalSessionWithCoordinates_When_ClickingViewOnMap_Then_TogglesMapPopover', () => {
      setup({ type: SessionType.Physical, locationName: 'Corp C, sala C309', latitude: 47.17, longitude: 27.57 });

      expect(query('location-info')?.textContent).toContain('Corp C, sala C309');
      expect(query('meet-btn')).toBeNull();
      expect(query('map-popover')).toBeNull();

      const mapBtn = query('map-btn') as HTMLButtonElement;
      expect(mapBtn.textContent?.trim()).toBe('View on map');
      mapBtn.click();
      fixture.detectChanges();
      expect(query('map-popover')).not.toBeNull();
      expect(query('viewer-address')?.textContent).toBe('Corp C, sala C309');
      expect(mapBtn.getAttribute('aria-expanded')).toBe('true');

      query('map-popover')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();
      expect(query('map-popover')).toBeNull();
    });

    it('Given_PhysicalSessionWithoutCoordinates_When_Rendering_Then_ShowsAddressWithoutMapButton', () => {
      setup({ type: SessionType.Physical, locationName: 'Biblioteca Centrala' });

      expect(query('location-info')?.textContent).toContain('Biblioteca Centrala');
      expect(query('map-btn')).toBeNull();
    });
  });

  describe('Rating', () => {
    const query = (id: string): HTMLElement | null => fixture.nativeElement.querySelector(`[data-testid="${id}"]`);
    const filledStars = (): number => fixture.nativeElement.querySelectorAll('[data-testid="rating-summary"] .star-filled').length;
    const allStars = (): number => fixture.nativeElement.querySelectorAll('[data-testid="rating-summary"] .star').length;

    it('Given_CompletedSessionNotRated_When_RenderingAsStudent_Then_ShowsActiveRateButtonAndNoRatingSummary', () => {
      setup({ status: SessionStatus.Completed, hasRated: false }, 'student');

      const rateBtn = query('rate-btn') as HTMLButtonElement;
      expect(rateBtn.textContent?.trim()).toBe('Rate Tutor');
      expect(rateBtn.disabled).toBe(false);
      expect(query('rating-summary')).toBeNull();
    });

    it('Given_CompletedSessionWithoutRatingInfo_When_RenderingAsStudent_Then_StillOffersRateButton', () => {
      setup({ status: SessionStatus.Completed }, 'student');

      expect(query('rate-btn')).not.toBeNull();
      expect(query('rating-summary')).toBeNull();
    });

    it('Given_RatedCompletedSession_When_RenderingAsStudent_Then_HidesRateButtonAndShowsScoreStarsAndComment', () => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: 5, ratingNotes: 'Explains very clearly' }, 'student');

      expect(query('rate-btn')).toBeNull();
      expect(query('rating-score')?.textContent?.trim()).toBe('5/5');
      expect(allStars()).toBe(5);
      expect(filledStars()).toBe(5);
      expect(query('rating-comment')?.textContent?.trim()).toBe('Explains very clearly');
      expect(query('rating-summary')?.textContent).toContain('Your rating');
    });

    it.each([1, 2, 3, 4])('Given_RatingScoreOf_%s_When_Rendering_Then_FillsExactlyThatManyStars', (score) => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: score }, 'student');

      expect(filledStars()).toBe(score);
      expect(allStars()).toBe(5);
      expect(query('rating-score')?.textContent?.trim()).toBe(`${score}/5`);
      expect(query('rating-summary')?.querySelector('[role="img"]')?.getAttribute('aria-label')).toBe(
        `Rated ${score} out of 5`
      );
    });

    it('Given_RatedSessionWithoutComment_When_Rendering_Then_ShowsStarsWithoutCommentBlock', () => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: 4, ratingNotes: null }, 'student');

      expect(query('rating-score')).not.toBeNull();
      expect(query('rating-comment')).toBeNull();
    });

    it('Given_RatedSessionWithBlankComment_When_Rendering_Then_HidesCommentBlock', () => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: 4, ratingNotes: '   ' }, 'student');

      expect(query('rating-comment')).toBeNull();
    });

    it('Given_RatedSessionWithoutScore_When_Rendering_Then_ShowsRatedLabelAndNoRateButton', () => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: null }, 'student');

      expect(query('rating-no-score')?.textContent?.trim()).toBe('Rated');
      expect(query('rating-score')).toBeNull();
      expect(query('rate-btn')).toBeNull();
    });

    it('Given_RatedCompletedSession_When_RenderingAsTutor_Then_ShowsStudentRatingWithoutRateButton', () => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: 3, ratingNotes: 'Good' }, 'tutor');

      expect(query('rate-btn')).toBeNull();
      expect(query('rating-summary')?.textContent).toContain('Student rating');
      expect(query('rating-score')?.textContent?.trim()).toBe('3/5');
      expect(query('rating-comment')?.textContent?.trim()).toBe('Good');
    });

    it('Given_RatedSession_When_RateIsRequestedProgrammatically_Then_DoesNotEmitRateRequested', () => {
      setup({ status: SessionStatus.Completed, hasRated: true, ratingScore: 5 }, 'student');
      const emitted: SessionResponse[] = [];
      component.rateRequested.subscribe((session) => emitted.push(session));

      component.onRate();

      expect(component.canRate()).toBe(false);
      expect(emitted).toEqual([]);
    });

    it('Given_UnratedCompletedSession_When_RateIsRequested_Then_EmitsRateRequested', () => {
      setup({ status: SessionStatus.Completed, hasRated: false }, 'student');
      const emitted: SessionResponse[] = [];
      component.rateRequested.subscribe((session) => emitted.push(session));

      component.onRate();

      expect(emitted.map((session) => session.id)).toEqual(['session-1']);
    });

    it('Given_SessionThatIsNotCompleted_When_Rendering_Then_ShowsNoRatingSummary', () => {
      setup({ status: SessionStatus.Booked, hasRated: true, ratingScore: 5 }, 'student');

      expect(query('rating-summary')).toBeNull();
    });

    it('Given_SessionBecomesRated_When_InputUpdates_Then_SwitchesFromRateButtonToRatingSummary', () => {
      setup({ status: SessionStatus.Completed, hasRated: false }, 'student');
      expect(query('rate-btn')).not.toBeNull();

      fixture.componentRef.setInput('session', {
        ...baseSession,
        status: SessionStatus.Completed,
        hasRated: true,
        ratingScore: 4,
        ratingNotes: 'Great'
      });
      fixture.detectChanges();

      expect(query('rate-btn')).toBeNull();
      expect(query('rating-score')?.textContent?.trim()).toBe('4/5');
    });
  });
});
