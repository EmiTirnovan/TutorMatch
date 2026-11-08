import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CancelSessionModalComponent } from '../../../../../src/app/features/sessions/components/cancel-session-modal/cancel-session-modal.component';
import { SessionService } from '../../../../../src/app/core/services/session.service';
import { TimeProvider } from '../../../../../src/app/core/services/time-provider.service';
import { SessionResponse, SessionStatus } from '../../../../../src/app/core/models/session.models';

describe('CancelSessionModalComponent', () => {
  let component: CancelSessionModalComponent;
  let fixture: ComponentFixture<CancelSessionModalComponent>;
  let sessionService: {
    cancelSession: ReturnType<typeof vi.fn>;
    completeSession: ReturnType<typeof vi.fn>;
    bookSession: ReturnType<typeof vi.fn>;
    getBookedSessionsForTutor: ReturnType<typeof vi.fn>;
    getMySessions: ReturnType<typeof vi.fn>;
  };
  let timeProvider: {
    now: ReturnType<typeof vi.fn>;
    isFreeCancellation: ReturnType<typeof vi.fn>;
    isLateCancellation: ReturnType<typeof vi.fn>;
    isEligibleToComplete: ReturnType<typeof vi.fn>;
    hoursUntilStart: ReturnType<typeof vi.fn>;
  };

  const mockSession: SessionResponse = {
    id: 'session-123',
    tutorId: 'tutor-123',
    studentId: 'student-123',
    startTime: '2026-11-10T14:00:00Z',
    endTime: '2026-11-10T15:00:00Z',
    status: SessionStatus.Booked,
    version: 1
  };

  beforeEach(async () => {
    sessionService = {
      cancelSession: vi.fn(),
      completeSession: vi.fn(),
      bookSession: vi.fn(),
      getBookedSessionsForTutor: vi.fn(),
      getMySessions: vi.fn()
    };

    timeProvider = {
      now: vi.fn().mockReturnValue(new Date('2026-11-09T00:00:00Z')),
      isFreeCancellation: vi.fn(),
      isLateCancellation: vi.fn(),
      isEligibleToComplete: vi.fn(),
      hoursUntilStart: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CancelSessionModalComponent],
      providers: [
        { provide: SessionService, useValue: sessionService },
        { provide: TimeProvider, useValue: timeProvider }
      ]
    }).compileComponents();
  });

  function createComponent(isLate: boolean = false): void {
    timeProvider.isLateCancellation.mockReturnValue(isLate);
    timeProvider.isFreeCancellation.mockReturnValue(!isLate);
    timeProvider.hoursUntilStart.mockReturnValue(isLate ? 5 : 24);

    fixture = TestBed.createComponent(CancelSessionModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('session', mockSession);
    fixture.componentRef.setInput('requesterId', 'student-123');
    fixture.detectChanges();
  }

  describe('Cancellation Warning Conditions (R3 Policy)', () => {
    it('Given_MoreThanOrEqual12HoursBeforeStart_When_ModalOpens_Then_RendersFreeCancellationNoticeAndNoAmberAlert', () => {
      // >= 12h: standard confirmation ("Cancellation is free")
      createComponent(false);

      const compiled = fixture.nativeElement as HTMLElement;
      const freeNotice = compiled.querySelector('[data-testid="free-cancellation-notice"]');
      const lateWarning = compiled.querySelector('[data-testid="late-cancellation-warning"]');

      expect(freeNotice).not.toBeNull();
      expect(freeNotice?.textContent).toContain('Cancellation is free');
      expect(lateWarning).toBeNull();
    });

    it('Given_LessThan12HoursBeforeStart_When_ModalOpens_Then_RendersAmberAlertWarningMarkedAsLateCancelled', () => {
      // < 12h: amber alert warning that cancellation will be marked as LateCancelled
      createComponent(true);

      const compiled = fixture.nativeElement as HTMLElement;
      const freeNotice = compiled.querySelector('[data-testid="free-cancellation-notice"]');
      const lateWarning = compiled.querySelector('[data-testid="late-cancellation-warning"]');

      expect(freeNotice).toBeNull();
      expect(lateWarning).not.toBeNull();
      expect(lateWarning?.classList.contains('alert-warning')).toBe(true);
      expect(lateWarning?.textContent).toContain('Late Cancellation Warning');
      expect(lateWarning?.textContent).toContain('LateCancelled');
    });
  });

  describe('Cancellation Actions', () => {
    it('Given_ConfirmedCancellation_When_ApiSucceeds_Then_EmitsCancelledAndClosed', () => {
      createComponent(false);
      sessionService.cancelSession.mockReturnValue(of(undefined as unknown as void));

      let cancelledEmitted: SessionResponse | undefined;
      let closedEmitted = false;
      component.cancelled.subscribe(val => (cancelledEmitted = val));
      component.closed.subscribe(() => (closedEmitted = true));

      const confirmBtn = fixture.nativeElement.querySelector('button.btn-danger') as HTMLButtonElement;
      confirmBtn.click();

      expect(sessionService.cancelSession).toHaveBeenCalledWith('session-123', 'student-123');
      expect(cancelledEmitted).toEqual(mockSession);
      expect(closedEmitted).toBe(true);
    });

    it('Given_ConfirmedCancellation_When_ApiFails_Then_DisplaysErrorMessageAndDoesNotEmitCancelled', () => {
      createComponent(false);
      const errorResponse = {
        error: { description: 'Session cannot be cancelled at this time.' }
      };
      sessionService.cancelSession.mockReturnValue(throwError(() => errorResponse));

      let cancelledEmitted = false;
      component.cancelled.subscribe(() => (cancelledEmitted = true));

      const confirmBtn = fixture.nativeElement.querySelector('button.btn-danger') as HTMLButtonElement;
      confirmBtn.click();
      fixture.detectChanges();

      expect(cancelledEmitted).toBe(false);
      expect(component.errorMessage()).toBe('Session cannot be cancelled at this time.');
      const errorAlert = fixture.nativeElement.querySelector('.alert-danger');
      expect(errorAlert?.textContent).toContain('Session cannot be cancelled at this time.');
    });

    it('Given_DismissClicked_When_ClickingKeepSession_Then_EmitsClosed', () => {
      createComponent(false);
      let closedEmitted = false;
      component.closed.subscribe(() => (closedEmitted = true));

      const dismissBtn = fixture.nativeElement.querySelector('button.btn-secondary') as HTMLButtonElement;
      dismissBtn.click();

      expect(closedEmitted).toBe(true);
    });

    it('Given_EscapeKey_When_Pressed_Then_EmitsClosed', () => {
      createComponent(false);
      let closedEmitted = false;
      component.closed.subscribe(() => (closedEmitted = true));

      const event = new KeyboardEvent('keydown', { key: 'Escape' });
      component.onKeydown(event);

      expect(closedEmitted).toBe(true);
    });

    it('Given_BackdropClick_When_ClickedOutsideDialog_Then_EmitsClosed', () => {
      createComponent(false);
      let closedEmitted = false;
      component.closed.subscribe(() => (closedEmitted = true));

      const backdrop = fixture.nativeElement.querySelector('.modal-backdrop') as HTMLElement;
      backdrop.click();

      expect(closedEmitted).toBe(true);
    });
  });
});
