import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MySessionsComponent } from '../../../../../src/app/features/sessions/components/my-sessions/my-sessions.component';
import { SessionService } from '../../../../../src/app/core/services/session.service';
import { UserContextService } from '../../../../../src/app/core/services/user-context.service';
import { TimeProvider } from '../../../../../src/app/core/services/time-provider.service';
import { SessionResponse, SessionStatus } from '../../../../../src/app/core/models/session.models';

describe('MySessionsComponent', () => {
  let component: MySessionsComponent;
  let fixture: ComponentFixture<MySessionsComponent>;
  let sessionService: {
    getMySessions: ReturnType<typeof vi.fn>;
    cancelSession: ReturnType<typeof vi.fn>;
    completeSession: ReturnType<typeof vi.fn>;
    bookSession: ReturnType<typeof vi.fn>;
    getBookedSessionsForTutor: ReturnType<typeof vi.fn>;
  };
  let timeProvider: {
    now: ReturnType<typeof vi.fn>;
    isFreeCancellation: ReturnType<typeof vi.fn>;
    isLateCancellation: ReturnType<typeof vi.fn>;
    isEligibleToComplete: ReturnType<typeof vi.fn>;
    hoursUntilStart: ReturnType<typeof vi.fn>;
  };
  let userContext: UserContextService;

  const mockSessions: SessionResponse[] = [
    {
      id: 'session-upcoming-1',
      tutorId: 'tutor-1',
      studentId: 'student-1',
      startTime: '2026-11-12T10:00:00Z', // Future
      endTime: '2026-11-12T11:00:00Z',
      status: SessionStatus.Booked,
      version: 1
    },
    {
      id: 'session-ongoing-1',
      tutorId: 'tutor-1',
      studentId: 'student-1',
      startTime: '2026-11-08T10:00:00Z', // Started / Past pending
      endTime: '2026-11-08T11:00:00Z',
      status: SessionStatus.Booked,
      version: 1
    },
    {
      id: 'session-completed-1',
      tutorId: 'tutor-1',
      studentId: 'student-1',
      startTime: '2026-11-05T10:00:00Z',
      endTime: '2026-11-05T11:00:00Z',
      status: SessionStatus.Completed,
      version: 2
    },
    {
      id: 'session-cancelled-1',
      tutorId: 'tutor-1',
      studentId: 'student-1',
      startTime: '2026-11-06T14:00:00Z',
      endTime: '2026-11-06T15:00:00Z',
      status: SessionStatus.Cancelled,
      version: 2
    },
    {
      id: 'session-late-cancelled-1',
      tutorId: 'tutor-1',
      studentId: 'student-1',
      startTime: '2026-11-07T14:00:00Z',
      endTime: '2026-11-07T15:00:00Z',
      status: SessionStatus.LateCancelled,
      version: 2
    }
  ];

  beforeEach(async () => {
    sessionService = {
      getMySessions: vi.fn().mockReturnValue(of(mockSessions)),
      cancelSession: vi.fn(),
      completeSession: vi.fn(),
      bookSession: vi.fn(),
      getBookedSessionsForTutor: vi.fn()
    };

    timeProvider = {
      now: vi.fn().mockReturnValue(new Date('2026-11-08T19:00:00Z')),
      isFreeCancellation: vi.fn().mockReturnValue(true),
      isLateCancellation: vi.fn().mockReturnValue(false),
      // Only sessions starting on or before 2026-11-08 are eligible to complete (has started)
      isEligibleToComplete: vi.fn((startTime: string | Date) => {
        const startStr = typeof startTime === 'string' ? startTime : startTime.toISOString();
        return startStr.includes('2026-11-08') || startStr.includes('2026-11-05');
      }),
      hoursUntilStart: vi.fn().mockReturnValue(24)
    };

    await TestBed.configureTestingModule({
      imports: [MySessionsComponent],
      providers: [
        provideRouter([]),
        UserContextService,
        { provide: SessionService, useValue: sessionService },
        { provide: TimeProvider, useValue: timeProvider }
      ]
    }).compileComponents();

    userContext = TestBed.inject(UserContextService);
    userContext.setRole('tutor');

    fixture = TestBed.createComponent(MySessionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('Given_LoadingState_When_Rendering_Then_DisplaysSkeletonLoaders', () => {
    component.isLoading.set(true);
    fixture.detectChanges();

    const skeletons = fixture.nativeElement.querySelector('[data-testid="skeleton-loaders"]');
    expect(skeletons).not.toBeNull();
  });

  it('Given_SessionsLoaded_When_RenderingTabs_Then_DisplaysAccurateCategoryCounts', () => {
    const upcomingTab = fixture.nativeElement.querySelector('[data-testid="tab-upcoming"]');
    const ongoingTab = fixture.nativeElement.querySelector('[data-testid="tab-ongoing"]');
    const completedTab = fixture.nativeElement.querySelector('[data-testid="tab-completed"]');
    const cancelledTab = fixture.nativeElement.querySelector('[data-testid="tab-cancelled"]');

    expect(upcomingTab?.textContent).toContain('1'); // 1 Future Booked
    expect(ongoingTab?.textContent).toContain('1'); // 1 Ongoing / Started Booked
    expect(completedTab?.textContent).toContain('1'); // 1 Completed
    expect(cancelledTab?.textContent).toContain('2'); // 1 Cancelled + 1 LateCancelled
  });

  it('Given_UpcomingTabActive_When_Viewing_Then_OnlyFutureSessionsAreDisplayed', () => {
    component.setTab('upcoming');
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('app-session-card');
    expect(cards.length).toBe(1);
    expect(component.visibleSessions().length).toBe(1);
    expect(component.visibleSessions()[0].id).toBe('session-upcoming-1');
  });

  it('Given_OngoingTabActive_When_ViewingAsTutor_Then_DisplaysOngoingSessionsAndTutorGuidance', () => {
    component.setTab('ongoing');
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('app-session-card');
    expect(cards.length).toBe(1);
    expect(component.visibleSessions().length).toBe(1);
    expect(component.visibleSessions()[0].id).toBe('session-ongoing-1');

    const guidance = fixture.nativeElement.querySelector('[data-testid="tutor-ongoing-guidance"]');
    expect(guidance).not.toBeNull();
    expect(guidance?.textContent).toContain('Tutor: Sessions Awaiting Completion');
  });

  it('Given_OngoingTabActive_When_ViewingAsStudent_Then_DisplaysStudentGuidance', () => {
    userContext.setRole('student');
    component.setTab('ongoing');
    fixture.detectChanges();

    const guidance = fixture.nativeElement.querySelector('[data-testid="student-ongoing-guidance"]');
    expect(guidance).not.toBeNull();
    expect(guidance?.textContent).toContain('Student: Ongoing Sessions');
  });

  it('Given_CompletedTabActive_When_Viewing_Then_OnlyCompletedSessionsAreDisplayed', () => {
    component.setTab('completed');
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('app-session-card');
    expect(cards.length).toBe(1);
    expect(component.visibleSessions().length).toBe(1);
    expect(component.visibleSessions()[0].id).toBe('session-completed-1');
  });

  it('Given_CancelledTabActive_When_Viewing_Then_BothCancelledAndLateCancelledAreDisplayed', () => {
    component.setTab('cancelled');
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('app-session-card');
    expect(cards.length).toBe(2);
    expect(component.visibleSessions().length).toBe(2);
    const ids = component.visibleSessions().map(s => s.id);
    expect(ids).toContain('session-cancelled-1');
    expect(ids).toContain('session-late-cancelled-1');
  });

  describe('Empty States', () => {
    beforeEach(() => {
      component.sessions.set([]);
      component.isLoading.set(false);
    });

    it('Given_EmptyUpcomingTab_When_Viewing_Then_RendersUpcomingEmptyState', () => {
      component.setTab('upcoming');
      fixture.detectChanges();

      const empty = fixture.nativeElement.querySelector('[data-testid="empty-state"]');
      expect(empty?.textContent).toContain('No upcoming sessions');
    });

    it('Given_EmptyOngoingTab_When_Viewing_Then_RendersOngoingEmptyState', () => {
      component.setTab('ongoing');
      fixture.detectChanges();

      const empty = fixture.nativeElement.querySelector('[data-testid="empty-state"]');
      expect(empty?.textContent).toContain('No ongoing sessions');
    });

    it('Given_EmptyCompletedTab_When_Viewing_Then_RendersCompletedEmptyState', () => {
      component.setTab('completed');
      fixture.detectChanges();

      const empty = fixture.nativeElement.querySelector('[data-testid="empty-state"]');
      expect(empty?.textContent).toContain('No completed sessions');
    });

    it('Given_EmptyCancelledTab_When_Viewing_Then_RendersCancelledEmptyState', () => {
      component.setTab('cancelled');
      fixture.detectChanges();

      const empty = fixture.nativeElement.querySelector('[data-testid="empty-state"]');
      expect(empty?.textContent).toContain('No cancelled sessions');
    });
  });

  describe('Error handling and retry', () => {
    it('Given_ApiFailure_When_LoadingSessions_Then_DisplaysErrorAlertWithRetryButton', () => {
      sessionService.getMySessions.mockReturnValue(throwError(() => ({ message: 'Network error' })));
      component.loadSessions();
      fixture.detectChanges();

      const errorAlert = fixture.nativeElement.querySelector('[data-testid="error-alert"]');
      expect(errorAlert).not.toBeNull();
      expect(errorAlert?.textContent).toContain('Network error');

      sessionService.getMySessions.mockReturnValue(of(mockSessions));
      const retryBtn = errorAlert?.querySelector('button');
      retryBtn?.click();
      fixture.detectChanges();

      expect(component.errorMessage()).toBeNull();
      expect(component.sessions().length).toBe(mockSessions.length);
    });
  });

  describe('Demo User Switcher', () => {
    it('Given_RoleToggle_When_Clicked_Then_SwitchesRoleAndReloadsSessions', () => {
      expect(component.currentRole()).toBe('tutor');

      const toggleBtn = fixture.nativeElement.querySelector('[data-testid="dashboard-toggle-role-btn"]') as HTMLButtonElement;
      toggleBtn.click();
      fixture.detectChanges();

      expect(component.currentRole()).toBe('student');
      expect(sessionService.getMySessions).toHaveBeenCalledWith(
        userContext.studentId(),
        'Student'
      );
    });
  });

  describe('Cancellation Flow', () => {
    it('Given_Session_When_CancelRequested_Then_OpensCancelModal', () => {
      component.openCancelModal(mockSessions[0]);
      fixture.detectChanges();

      expect(component.sessionToCancel()).toEqual(mockSessions[0]);
      const modal = fixture.nativeElement.querySelector('app-cancel-session-modal');
      expect(modal).not.toBeNull();
    });

    it('Given_SessionCancelled_When_ModalEmits_Then_ClosesModalAndShowsSuccessAlert', () => {
      component.openCancelModal(mockSessions[0]);
      component.onSessionCancelled(mockSessions[0]);
      fixture.detectChanges();

      expect(component.sessionToCancel()).toBeNull();
      const successAlert = fixture.nativeElement.querySelector('[data-testid="action-success-alert"]');
      expect(successAlert?.textContent).toContain('cancelled successfully');
    });
  });

  describe('Complete Session Flow', () => {
    it('Given_Tutor_When_CompletingSession_Then_CallsCompleteApiAndShowsSuccessAlert', () => {
      sessionService.completeSession.mockReturnValue(of(undefined as unknown as void));

      component.onCompleteSession(mockSessions[1]);
      fixture.detectChanges();

      expect(sessionService.completeSession).toHaveBeenCalledWith(
        mockSessions[1].id,
        userContext.tutorId()
      );
      const successAlert = fixture.nativeElement.querySelector('[data-testid="action-success-alert"]');
      expect(successAlert?.textContent).toContain('marked as completed');
    });
  });
});
