import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { LatLng, LocationMapComponent } from '../location-map/location-map.component';

export interface PickedLocation extends LatLng {
  address: string;
}

/**
 * Interactive map where the user searches an address or clicks the map to place the pin.
 * Emits only verified locations (a geocoded search result or a reverse-geocoded click), never free text.
 */
@Component({
  selector: 'app-location-picker-map',
  standalone: true,
  imports: [FormsModule, LocationMapComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './location-picker-map.component.html',
  styleUrl: './location-picker-map.component.css',
})
export class LocationPickerMapComponent {
  private readonly geocoding = inject(GeocodingService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly mapClicks = new Subject<LatLng>();

  /** The verified location, or null once it no longer matches what the user typed. */
  readonly locationChange = output<PickedLocation | null>();
  /** True while an address lookup is running, so the parent can hold off submitting. */
  readonly busyChange = output<boolean>();
  /** Apartment, floor etc. removed from the search text, for the parent's details field. */
  readonly unitDetailsFound = output<string>();

  readonly address = signal<string>('');
  readonly pickedPoint = signal<LatLng | null>(null);
  readonly searchResults = signal<GeocodedPlace[] | null>(null);
  readonly isGeocoding = signal(false);
  readonly locationError = signal<string | null>(null);
  readonly locationHint = signal<string | null>(null);

  constructor() {
    // Debounced so fast map clicks stay within Nominatim's 1 request/second policy.
    this.mapClicks
      .pipe(
        tap(() => this.setBusy(true)),
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
        this.setBusy(false);
        if (place) {
          this.address.set(place.address);
          this.setPoint(point);
          this.locationError.set(null);
        } else {
          this.setPoint(null);
          this.locationError.set(
            failed
              ? 'We could not verify this location. Please try again.'
              : 'There is no known address at that point. Pick another spot.',
          );
        }
      });
  }

  /** Called by the parent form when it is submitted without a location. */
  markMissing(): void {
    this.locationError.set('Search the address and choose it, or pin the spot on the map.');
  }

  onAddressInput(value: string): void {
    // Typed text no longer matches the verified pin, so the user has to search again.
    this.address.set(value);
    this.setPoint(null);
    this.locationHint.set(null);
    this.searchResults.set(null);
  }

  searchAddress(): void {
    // Apartment, floor etc. are not in OpenStreetMap and make the lookup fail, so hand them to the parent.
    const { query, details } = splitAddress(this.address());
    if (details) {
      this.unitDetailsFound.emit(details);
      this.address.set(query);
    }

    const attempts = fallbackQueries(query);
    if (attempts.length === 0) {
      this.locationError.set('Type the street and number or the building name, then search.');
      return;
    }

    this.setBusy(true);
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
        // The picker disappears when the user switches to online; drop the lookup with it.
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.setBusy(false);
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
          this.setBusy(false);
          this.locationError.set('We could not search the address right now. Please try again.');
        },
      });
  }

  choosePlace(place: GeocodedPlace): void {
    this.address.set(place.address);
    this.setPoint({ lat: place.lat, lng: place.lng });
    this.searchResults.set(null);
    this.locationError.set(null);
  }

  onMapPicked(point: LatLng): void {
    this.setPoint(null);
    this.locationHint.set(null);
    this.searchResults.set(null);
    this.mapClicks.next(point);
  }

  private setPoint(point: LatLng | null): void {
    this.pickedPoint.set(point);
    this.locationChange.emit(point && { ...point, address: this.address() });
  }

  private setBusy(busy: boolean): void {
    this.isGeocoding.set(busy);
    this.busyChange.emit(busy);
  }
}
