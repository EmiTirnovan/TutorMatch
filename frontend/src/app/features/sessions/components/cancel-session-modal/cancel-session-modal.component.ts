import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { SessionResponse } from '../../../../core/models/session.models';
import { SessionService } from '../../../../core/services/session.service';
import { TimeProvider } from '../../../../core/services/time-provider.service';
import { keepTabFocusInside, restoreFocusOnDestroy } from '../../../../core/utils/modal-focus';

@Component({
  selector: 'app-cancel-session-modal',
  standalone: true,
  imports: [CommonModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cancel-session-modal.component.html',
  styleUrl: './cancel-session-modal.component.css'
})
export class CancelSessionModalComponent implements AfterViewInit {
  private readonly sessionService = inject(SessionService);
  readonly timeProvider = inject(TimeProvider);

  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');

  readonly session = input.required<SessionResponse>();
  readonly requesterId = input.required<string>();

  readonly cancelled = output<SessionResponse>();
  readonly closed = output<void>();

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  /**
   * Evaluates if this cancellation is late (< 12 hours before start), matching backend TimeProvider rules.
   */
  readonly isLate = computed(() =>
    this.timeProvider.isLateCancellation(this.session().startTime)
  );

  readonly hoursRemaining = computed(() => {
    const hours = this.timeProvider.hoursUntilStart(this.session().startTime);
    return Math.max(0, Math.round(hours * 10) / 10);
  });

  constructor() {
    restoreFocusOnDestroy();
  }

  ngAfterViewInit(): void {
    this.dialog().nativeElement.focus();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' && !this.isSubmitting()) {
      event.preventDefault();
      this.close();
      return;
    }

    keepTabFocusInside(event, this.dialog().nativeElement);
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isSubmitting()) {
      this.close();
    }
  }

  confirmCancellation(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.sessionService.cancelSession(this.session().id, this.requesterId()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.cancelled.emit(this.session());
        this.closed.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        const description =
          err.error?.description ||
          err.error?.detail ||
          err.message ||
          'Failed to cancel session. Please try again.';
        this.errorMessage.set(description);
      }
    });
  }

  close(): void {
    if (!this.isSubmitting()) {
      this.closed.emit();
    }
  }
}

