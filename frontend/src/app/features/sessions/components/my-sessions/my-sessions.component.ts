import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  effect,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  SessionResponse,
  isCancelled,
  isCompleted,
  isLateCancelled,
  isUpcoming
} from '../../../../core/models/session.models';
import { SessionService } from '../../../../core/services/session.service';
import { UserContextService, UserRole } from '../../../../core/services/user-context.service';
import { TimeProvider } from '../../../../core/services/time-provider.service';
import { SessionCardComponent } from '../session-card/session-card.component';
import { CancelSessionModalComponent } from '../cancel-session-modal/cancel-session-modal.component';
import { RateSessionModalComponent } from '../../../tutors/components/rate-session-modal/rate-session-modal.component';

export type SessionTab = 'upcoming' | 'ongoing' | 'completed' | 'cancelled';

@Component({
  selector: 'app-my-sessions',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    SessionCardComponent,
    CancelSessionModalComponent,
    RateSessionModalComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-sessions.component.html',
  styleUrl: './my-sessions.component.css'
})
export class MySessionsComponent implements OnInit {
  private readonly sessionService = inject(SessionService);
  readonly userContext = inject(UserContextService);
  readonly timeProvider = inject(TimeProvider);

  readonly currentRole = this.userContext.currentRole;
  readonly activeTab = signal<SessionTab>('upcoming');

  readonly sessions = signal<SessionResponse[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly actionSuccessMessage = signal<string | null>(null);

  readonly skeletonItems = [1, 2, 3];

  // Active modals state
  readonly sessionToCancel = signal<SessionResponse | null>(null);
  readonly sessionToRate = signal<SessionResponse | null>(null);

  readonly allBookedSessions = computed(() =>
    this.sessions().filter(s => isUpcoming(s.status))
  );

  /**
   * Future sessions only (not started yet: now < startTime)
   */
  readonly upcomingSessions = computed(() =>
    this.allBookedSessions().filter(s => !this.timeProvider.isEligibleToComplete(s.startTime))
  );

  /**
   * Ongoing / started sessions awaiting completion (now >= startTime)
   */
  readonly ongoingSessions = computed(() =>
    this.allBookedSessions().filter(s => this.timeProvider.isEligibleToComplete(s.startTime))
  );

  readonly completedSessions = computed(() =>
    this.sessions().filter(s => isCompleted(s.status))
  );

  readonly cancelledSessions = computed(() =>
    this.sessions().filter(s => isCancelled(s.status) || isLateCancelled(s.status))
  );

  readonly visibleSessions = computed(() => {
    switch (this.activeTab()) {
      case 'upcoming':
        return this.upcomingSessions();
      case 'ongoing':
        return this.ongoingSessions();
      case 'completed':
        return this.completedSessions();
      case 'cancelled':
        return this.cancelledSessions();
    }
  });

  constructor() {
    // Whenever the active identity/role or user ID changes, reload sessions
    effect(() => {
      const role = this.currentRole();
      const userId = this.userContext.currentUserId();
      this.loadSessions(userId, role);
    });
  }

  ngOnInit(): void {
    // Initial load handled by effect
  }

  setTab(tab: SessionTab): void {
    this.activeTab.set(tab);
  }

  toggleRole(): void {
    this.userContext.toggleRole();
  }

  loadSessions(
    userId: string = this.userContext.currentUserId(),
    role: UserRole = this.currentRole()
  ): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const apiRole = role === 'student' ? 'Student' : 'Tutor';

    this.sessionService.getMySessions(userId, apiRole).subscribe({
      next: (sessions) => {
        this.sessions.set(sessions);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        const description =
          err.error?.description ||
          err.error?.detail ||
          err.message ||
          'Failed to load sessions. Please try again.';
        this.errorMessage.set(description);
      }
    });
  }

  retry(): void {
    this.loadSessions();
  }

  // Cancellation flow
  openCancelModal(session: SessionResponse): void {
    this.sessionToCancel.set(session);
  }

  closeCancelModal(): void {
    this.sessionToCancel.set(null);
  }

  onSessionCancelled(session: SessionResponse): void {
    this.sessionToCancel.set(null);
    this.actionSuccessMessage.set('Session was cancelled successfully.');
    this.loadSessions();
  }

  // Complete session flow (for tutor)
  onCompleteSession(session: SessionResponse): void {
    this.errorMessage.set(null);
    this.actionSuccessMessage.set(null);

    this.sessionService.completeSession(session.id, this.userContext.tutorId()).subscribe({
      next: () => {
        this.actionSuccessMessage.set('Session marked as completed successfully.');
        this.loadSessions();
      },
      error: (err: HttpErrorResponse) => {
        const description =
          err.error?.description ||
          err.error?.detail ||
          err.message ||
          'Failed to mark session as completed.';
        this.errorMessage.set(description);
      }
    });
  }

  // Rate session flow (for student)
  openRateModal(session: SessionResponse): void {
    this.sessionToRate.set(session);
  }

  closeRateModal(): void {
    this.sessionToRate.set(null);
  }

  onSessionRated(): void {
    this.sessionToRate.set(null);
    this.actionSuccessMessage.set('Thank you! Your rating has been submitted.');
    this.loadSessions();
  }

  dismissAlert(): void {
    this.actionSuccessMessage.set(null);
  }
}
