import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  inject,
  input,
  output,
  signal,
  viewChild
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { ApiError, DEMO_STUDENT_ID, SessionResponse } from '../../../../core/models/session.models';
import { SessionService } from '../../../../core/services/session.service';
import { keepTabFocusInside, restoreFocusOnDestroy } from '../../../../core/utils/modal-focus';

@Component({
  selector: 'app-book-session-modal',
  standalone: true,
  imports: [DatePipe, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './book-session-modal.component.html',
  styleUrl: './book-session-modal.component.css'
})
export class BookSessionModalComponent implements OnInit, AfterViewInit {
  private readonly sessionService = inject(SessionService);
  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');
  readonly tutor = input.required<TutorDetailsResponse>();
  readonly slot = input.required<DateRangeDto>();

  readonly booked = output<SessionResponse>();
  readonly conflict = output<void>();
  readonly closed = output<void>();

  readonly isSubmitting = signal(false);
  readonly dailyLimitReached = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Subject and details state
  readonly selectedSubjectType = signal<string>('__custom__');
  readonly customSubject = signal<string>('');
  readonly notes = signal<string>('');

  constructor() {
    restoreFocusOnDestroy();
  }

  ngOnInit(): void {
    const subjects = this.tutor().subjects;
    if (subjects && subjects.length > 0) {
      this.selectedSubjectType.set(subjects[0]);
    } else {
      this.selectedSubjectType.set('__custom__');
    }
  }

  ngAfterViewInit(): void {
    this.dialog().nativeElement.focus();
  }

  onKeydown(event: KeyboardEvent): void {
    keepTabFocusInside(event, this.dialog().nativeElement);
  }

  close(): void {
    // Closing mid-request would hide the outcome of a booking the server may still accept.
    if (!this.isSubmitting()) {
      this.closed.emit();
    }
  }

  confirm(): void {
    if (this.isSubmitting()) {
      return;
    }

    const isCustom = this.selectedSubjectType() === '__custom__' || !this.tutor().subjects?.length;
    const finalSubject = (isCustom ? this.customSubject() : this.selectedSubjectType()).trim();
    const finalNotes = this.notes().trim();

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.sessionService
      .bookSession(DEMO_STUDENT_ID, {
        tutorId: this.tutor().id,
        startTime: this.slot().start,
        endTime: this.slot().end,
        subject: finalSubject || undefined,
        notes: finalNotes || undefined
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
