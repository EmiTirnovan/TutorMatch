import { AfterViewInit, Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { ApiError, DEMO_STUDENT_ID, SessionResponse } from '../../../../core/models/session.models';
import { SessionService } from '../../../../core/services/session.service';

@Component({
  selector: 'app-book-session-modal',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './book-session-modal.component.html'
})
export class BookSessionModalComponent implements AfterViewInit {
  private readonly sessionService = inject(SessionService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly tutor = input.required<TutorDetailsResponse>();
  readonly slot = input.required<DateRangeDto>();

  readonly booked = output<SessionResponse>();
  readonly conflict = output<void>();
  readonly closed = output<void>();

  readonly isSubmitting = signal(false);
  readonly dailyLimitReached = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngAfterViewInit(): void {
    this.dialog().nativeElement.showModal();
  }

  onCancel(event: Event): void {
    // Closing mid-request would hide the outcome of a booking the server may still accept.
    if (this.isSubmitting()) {
      event.preventDefault();
    }
  }

  onClose(): void {
    // Browsers let a repeated Esc close the dialog even after preventDefault.
    if (this.isSubmitting()) {
      this.dialog().nativeElement.showModal();
    } else {
      this.closed.emit();
    }
  }

  confirm(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.sessionService
      .bookSession({
        tutorId: this.tutor().id,
        studentId: DEMO_STUDENT_ID,
        startTime: this.slot().start,
        endTime: this.slot().end
      })
      .subscribe({
        next: session => this.booked.emit(session),
        error: (err: HttpErrorResponse) => this.handleError(err)
      });
  }

  private handleError(err: HttpErrorResponse): void {
    this.isSubmitting.set(false);
    const apiError = err.error as ApiError | null;

    if (err.status === 409) {
      this.conflict.emit();
    } else if (apiError?.code === 'Session.DailyLimit') {
      this.dailyLimitReached.set(true);
    } else if (err.status === 400 || err.status === 404) {
      this.errorMessage.set('This slot can no longer be booked. Close this window and choose another time.');
    } else {
      this.errorMessage.set('We could not complete your booking. Please try again.');
    }
  }
}
