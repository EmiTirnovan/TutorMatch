import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SessionCardComponent } from '../../../../../src/app/features/sessions/components/session-card/session-card.component';
import { TimeProvider } from '../../../../../src/app/core/services/time-provider.service';
import { SessionResponse, SessionStatus } from '../../../../../src/app/core/models/session.models';

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
});
