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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  Subject,
  catchError,
  concatMap,
  debounceTime,
  find,
  from,
  map,
  of,
  switchMap,
  tap,
  timer,
} from 'rxjs';
import { fallbackQueries, splitAddress } from '../../../../core/utils/address-parts';
import { GeocodedPlace, GeocodingService } from '../../../../core/services/geocoding.service';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { ApiError, SessionResponse, SessionType } from '../../../../core/models/session.models';
import { SessionService } from '../../../../core/services/session.service';
import { UserContextService } from '../../../../core/services/user-context.service';
import {
  LatLng,
  LocationMapComponent,
} from '../../../sessions/components/location-map/location-map.component';
import { keepTabFocusInside, restoreFocusOnDestroy } from '../../../../core/utils/modal-focus';

@Component({
  selector: 'app-book-session-modal',
  standalone: true,
  imports: [DatePipe, FormsModule, LocationMapComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './book-session-modal.component.html',
  styleUrl: './book-session-modal.component.css',
})
export class BookSessionModalComponent implements OnInit, AfterViewInit {
  private readonly sessionService = inject(SessionService);
  private readonly userContext = inject(UserContextService);
  private readonly geocoding = inject(GeocodingService);
  private readonly mapClicks = new Subject<LatLng>();
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
  readonly locationName = signal<string>('');
  readonly locationDetails = signal<string>('');
  /** Only set from a geocoded search result or a reverse-geocoded map click, never from free text. */
  readonly pickedPoint = signal<LatLng | null>(null);
  readonly searchResults = signal<GeocodedPlace[] | null>(null);
  readonly isGeocoding = signal(false);
  readonly locationError = signal<string | null>(null);
  readonly locationHint = signal<string | null>(null);

  constructor() {
    restoreFocusOnDestroy();

    // Debounced so fast map clicks stay within Nominatim's 1 request/second policy.
    this.mapClicks
      .pipe(
        tap(() => this.isGeocoding.set(true)),
        debounceTime(1000),
        switchMap((point) =>
          this.geocoding.reverse(point.lat, point.lng).pipe(
            map((place) => ({ point, place, failed: false })),
            catchError(() => of({ point, place: null, failed: true })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ point, place, failed }) => {
        this.isGeocoding.set(false);
        if (place) {
          this.locationName.set(place.address);
          this.pickedPoint.set(point);
          this.locationError.set(null);
        } else {
          this.pickedPoint.set(null);
          this.locationError.set(
            failed
              ? 'We could not verify this location. Please try again.'
              : 'There is no known address at that point. Pick another spot.',
          );
        }
      });
  }

  onAddressInput(value: string): void {
    // Typed text no longer matches the verified pin, so the student has to search again.
    this.locationName.set(value);
    this.pickedPoint.set(null);
    this.locationHint.set(null);
    this.searchResults.set(null);
  }

  searchAddress(): void {
    // Apartment, floor etc. are not in OpenStreetMap and make the lookup fail, so move them to the details field.
    const { query, details } = splitAddress(this.locationName());
    if (details) {
      const existing = this.locationDetails().trim();
      this.locationDetails.set(existing ? `${existing}, ${details}` : details);
      this.locationName.set(query);
    }

    const attempts = fallbackQueries(query);
    if (attempts.length === 0) {
      this.locationError.set('Type the street and number or the building name, then search.');
      return;
    }

    this.isGeocoding.set(true);
    this.locationError.set(null);
    this.locationHint.set(null);
    this.searchResults.set(null);
    from(attempts)
      .pipe(
        // Retries wait a second each to respect Nominatim's 1 request/second policy.
        concatMap((attempt, i) =>
          (i === 0 ? of(0) : timer(1000)).pipe(
            switchMap(() => this.geocoding.search(attempt.query)),
            map((places) => ({ ...attempt, places })),
          ),
        ),
        find((result) => result.places.length > 0),
      )
      .subscribe({
        next: (result) => {
          this.isGeocoding.set(false);
          if (!result) {
            this.locationError.set(
              'Address not found. Check the spelling or pin the spot on the map.',
            );
            return;
          }
          if (result.streetOnly) {
            this.locationHint.set(
              'We found the street but not the exact number. Click the map on the exact building to move the pin.',
            );
          }
          if (result.places.length === 1) {
            this.choosePlace(result.places[0]);
          } else {
            this.searchResults.set(result.places);
          }
        },
        error: () => {
          this.isGeocoding.set(false);
          this.locationError.set('We could not search the address right now. Please try again.');
        },
      });
  }

  choosePlace(place: GeocodedPlace): void {
    this.locationName.set(place.address);
    this.pickedPoint.set({ lat: place.lat, lng: place.lng });
    this.searchResults.set(null);
    this.locationError.set(null);
  }

  onMapPicked(point: LatLng): void {
    this.pickedPoint.set(null);
    this.locationHint.set(null);
    this.searchResults.set(null);
    this.mapClicks.next(point);
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
    const point = this.pickedPoint();
    const details = this.locationDetails().trim();
    // Column is 200 chars; geocoded names can be long, so shorten the address and keep the unit details whole.
    const suffix = details ? ` - ${details}` : '';
    const finalLocation =
      this.locationName()
        .trim()
        .slice(0, 200 - suffix.length) + suffix;

    if (isPhysical && !point) {
      this.locationError.set('Search the address and choose it, or pin the spot on the map.');
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
          latitude: point!.lat,
          longitude: point!.lng,
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
