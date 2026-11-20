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
  viewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { ApiError, SessionResponse, SessionType } from '../../../../core/models/session.models';
import { SessionService } from '../../../../core/services/session.service';
import { UserContextService } from '../../../../core/services/user-context.service';
import {
  LocationPickerMapComponent,
  PickedLocation,
} from '../../../locations/components/location-picker-map/location-picker-map.component';
import { keepTabFocusInside, restoreFocusOnDestroy } from '../../../../core/utils/modal-focus';

@Component({
  selector: 'app-book-session-modal',
  standalone: true,
  imports: [DatePipe, FormsModule, LocationPickerMapComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './book-session-modal.component.html',
  styleUrl: './book-session-modal.component.css',
})
export class BookSessionModalComponent implements OnInit, AfterViewInit {
  private readonly sessionService = inject(SessionService);
  private readonly userContext = inject(UserContextService);
  private readonly picker = viewChild(LocationPickerMapComponent);
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

  // Online vs physical meeting
  readonly SessionType = SessionType;
  readonly sessionType = signal<SessionType>(SessionType.Online);
  readonly locationDetails = signal<string>('');
  /** Verified by the picker (geocoded search or map click); null while missing or edited. */
  readonly pickedLocation = signal<PickedLocation | null>(null);
  readonly isGeocoding = signal(false);

  constructor() {
    restoreFocusOnDestroy();
  }

  selectOnline(): void {
    // The picker (and any lookup it was running) is removed, so its location no longer applies.
    this.sessionType.set(SessionType.Online);
    this.pickedLocation.set(null);
    this.isGeocoding.set(false);
  }

  onUnitDetailsFound(details: string): void {
    const existing = this.locationDetails().trim();
    this.locationDetails.set(existing ? `${existing}, ${details}` : details);
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
    if (this.isSubmitting() || this.isGeocoding()) {
      return;
    }

    const isCustom = this.selectedSubjectType() === '__custom__' || !this.tutor().subjects?.length;
    const finalSubject = (isCustom ? this.customSubject() : this.selectedSubjectType()).trim();
    const finalNotes = this.notes().trim();
    const isPhysical = this.sessionType() === SessionType.Physical;
    const location = this.pickedLocation();
    const details = this.locationDetails().trim();
    // Column is 200 chars; geocoded names can be long, so shorten the address and keep the unit details whole.
    const suffix = details ? ` - ${details}` : '';
    const finalLocation = (location?.address ?? '').trim().slice(0, 200 - suffix.length) + suffix;

    if (isPhysical && !location) {
      this.picker()?.markMissing();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.sessionService
      .bookSession(this.userContext.studentId(), {
        tutorId: this.tutor().id,
        startTime: this.slot().start,
        endTime: this.slot().end,
        subject: finalSubject || undefined,
        notes: finalNotes || undefined,
        type: this.sessionType(),
        ...(isPhysical && {
          locationName: finalLocation,
          latitude: location!.lat,
          longitude: location!.lng,
        }),
      })
      .subscribe({
        next: (session) => this.booked.emit(session),
        error: (err: HttpErrorResponse) => this.handleError(err),
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
      this.errorMessage.set(
        'This slot can no longer be booked. Close this window and choose another time.',
      );
    } else {
      this.errorMessage.set('We could not complete your booking. Please try again.');
    }
  }
}
