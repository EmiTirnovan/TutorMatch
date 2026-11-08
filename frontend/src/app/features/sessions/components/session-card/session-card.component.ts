import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  SessionResponse,
  getStatusLabel,
  isCancelled,
  isCompleted,
  isLateCancelled,
  isUpcoming
} from '../../../../core/models/session.models';
import { TimeProvider } from '../../../../core/services/time-provider.service';

@Component({
  selector: 'app-session-card',
  standalone: true,
  imports: [CommonModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './session-card.component.html',
  styleUrl: './session-card.component.css'
})
export class SessionCardComponent {
  private readonly timeProvider = inject(TimeProvider);

  readonly session = input.required<SessionResponse>();
  readonly userRole = input.required<'student' | 'tutor'>();
  readonly currentUserId = input<string | null>(null);

  readonly cancelRequested = output<SessionResponse>();
  readonly completeRequested = output<SessionResponse>();
  readonly rateRequested = output<SessionResponse>();

  readonly isUpcomingSession = computed(() => isUpcoming(this.session().status));
  readonly isCompletedSession = computed(() => isCompleted(this.session().status));
  readonly isCancelledSession = computed(() => isCancelled(this.session().status));
  readonly isLateCancelledSession = computed(() => isLateCancelled(this.session().status));

  /**
   * Has the session reached or passed its start time?
   */
  readonly hasStarted = computed(() =>
    this.timeProvider.isEligibleToComplete(this.session().startTime)
  );

  /**
   * Is the session strictly in the future (not started yet)?
   */
  readonly isFuture = computed(() => !this.hasStarted());

  /**
   * Is the current user an enrolled participant in this session?
   */
  readonly isParticipant = computed(() => {
    const uid = this.currentUserId();
    if (!uid) return true;
    return this.userRole() === 'student'
      ? this.session().studentId === uid
      : this.session().tutorId === uid;
  });

  /**
   * Cancellation is ONLY allowed for students on future sessions (before start time) they are enrolled in.
   * Tutors CANNOT cancel a student's booking.
   * Past or ongoing sessions CANNOT be cancelled.
   */
  readonly canCancel = computed(() =>
    this.userRole() === 'student' &&
    this.isUpcomingSession() &&
    this.isFuture() &&
    this.isParticipant()
  );

  /**
   * Only the tutor can mark a session as completed, and only once the session has started.
   */
  readonly canComplete = computed(() =>
    this.userRole() === 'tutor' &&
    this.isUpcomingSession() &&
    this.hasStarted() &&
    this.isParticipant()
  );

  /**
   * Only the student can rate a completed session.
   */
  readonly canRate = computed(() =>
    this.userRole() === 'student' &&
    this.isCompletedSession() &&
    this.isParticipant()
  );

  readonly statusLabel = computed(() => {
    if (this.isCompletedSession()) return 'Completed';
    if (this.isLateCancelledSession()) return 'Late Cancelled';
    if (this.isCancelledSession()) return 'Cancelled';
    if (this.hasStarted()) {
      return this.userRole() === 'tutor' ? 'Needs Completion' : 'In Progress';
    }
    return 'Upcoming';
  });

  readonly statusBadgeClass = computed(() => {
    if (this.isLateCancelledSession()) return 'badge-warning';
    if (this.isCancelledSession()) return 'badge-neutral';
    if (this.isCompletedSession()) return 'badge-success';
    if (this.hasStarted()) return 'badge-warning';
    return 'badge-primary';
  });

  onCancel(): void {
    if (this.canCancel()) {
      this.cancelRequested.emit(this.session());
    }
  }

  onComplete(): void {
    if (this.canComplete()) {
      this.completeRequested.emit(this.session());
    }
  }

  onRate(): void {
    if (this.canRate()) {
      this.rateRequested.emit(this.session());
    }
  }
}
