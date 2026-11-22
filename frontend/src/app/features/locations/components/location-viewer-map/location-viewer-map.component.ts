import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LocationMapComponent } from '../location-map/location-map.component';

/** Read-only map with the meeting pin and its address, for session details. */
@Component({
  selector: 'app-location-viewer-map',
  standalone: true,
  imports: [LocationMapComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-location-map [latitude]="latitude()" [longitude]="longitude()" />
    @if (address()) {
      <p class="viewer-address" data-testid="viewer-address">{{ address() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
      width: 100%;
    }
    .viewer-address {
      margin: 0.375rem 0 0;
      font-size: 0.8125rem;
      color: var(--slate-500);
    }
  `,
})
export class LocationViewerMapComponent {
  readonly latitude = input.required<number>();
  readonly longitude = input.required<number>();
  readonly address = input<string | null | undefined>(null);
}
