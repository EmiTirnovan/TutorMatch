import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  effect,
  input,
  output,
  viewChild
} from '@angular/core';
import type * as Leaflet from 'leaflet';

export interface LatLng {
  lat: number;
  lng: number;
}

// Default view when no point is chosen yet (Iasi city centre).
const DEFAULT_CENTER: LatLng = { lat: 47.1585, lng: 27.6014 };

@Component({
  selector: 'app-location-map',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div #map class="location-map" role="application" [attr.aria-label]="editable() ? 'Map: click to pick the meeting point' : 'Map showing the meeting point'"></div>`,
  styles: `:host { display: block; width: 100%; } .location-map { height: 220px; width: 100%; border-radius: 8px; border: 1px solid var(--slate-200); }`
})
export class LocationMapComponent implements OnDestroy {
  readonly latitude = input<number | null | undefined>(null);
  readonly longitude = input<number | null | undefined>(null);
  readonly editable = input(false);
  readonly picked = output<LatLng>();

  private readonly host = viewChild.required<ElementRef<HTMLElement>>('map');
  private L?: typeof Leaflet;
  private map?: Leaflet.Map;
  private marker?: Leaflet.CircleMarker;
  private destroyed = false;

  constructor() {
    afterNextRender(() => void this.init());
    // The parent owns the point: the marker follows the inputs (search result, verified click, or cleared).
    effect(() => this.showPoint(this.latitude(), this.longitude()));
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.map?.remove();
  }

  private async init(): Promise<void> {
    // Loaded lazily so Leaflet only ships when a map is actually shown.
    const L = await import('leaflet');
    if (this.destroyed) return;

    this.L = L;
    this.map = L.map(this.host().nativeElement).setView(DEFAULT_CENTER, 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(this.map);

    if (this.editable()) {
      this.map.on('click', e => this.picked.emit({ lat: e.latlng.lat, lng: e.latlng.lng }));
    }
    this.showPoint(this.latitude(), this.longitude());
  }

  private showPoint(lat: number | null | undefined, lng: number | null | undefined): void {
    const { L, map } = this;
    if (!L || !map) return;

    if (lat == null || lng == null) {
      this.marker?.remove();
      this.marker = undefined;
      return;
    }
    // circleMarker avoids Leaflet's default icon images, which need extra asset config.
    this.marker ??= L.circleMarker([lat, lng], { radius: 9 }).addTo(map);
    this.marker.setLatLng([lat, lng]);
    map.setView([lat, lng], Math.max(map.getZoom(), 16));
  }
}
