import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  input,
  output,
  viewChild
} from '@angular/core';
import type { CircleMarker, Map as LeafletMap } from 'leaflet';

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
  styles: `.location-map { height: 220px; width: 100%; border-radius: 8px; border: 1px solid var(--slate-200); }`
})
export class LocationMapComponent implements OnDestroy {
  readonly latitude = input<number | null | undefined>(null);
  readonly longitude = input<number | null | undefined>(null);
  readonly editable = input(false);
  readonly picked = output<LatLng>();

  private readonly host = viewChild.required<ElementRef<HTMLElement>>('map');
  private map?: LeafletMap;
  private destroyed = false;

  constructor() {
    afterNextRender(() => void this.init());
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.map?.remove();
  }

  private async init(): Promise<void> {
    // Loaded lazily so Leaflet only ships when a map is actually shown.
    const L = await import('leaflet');
    if (this.destroyed) return;

    const lat = this.latitude();
    const lng = this.longitude();
    const hasPoint = lat != null && lng != null;
    const center: LatLng = hasPoint ? { lat, lng } : DEFAULT_CENTER;

    const map = L.map(this.host().nativeElement).setView(center, hasPoint ? 16 : 13);
    this.map = map;
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // circleMarker avoids Leaflet's default icon images, which need extra asset config.
    let marker: CircleMarker | undefined = hasPoint ? L.circleMarker(center, { radius: 9 }).addTo(map) : undefined;

    if (this.editable()) {
      map.on('click', e => {
        marker ??= L.circleMarker(e.latlng, { radius: 9 }).addTo(map);
        marker.setLatLng(e.latlng);
        this.picked.emit({ lat: e.latlng.lat, lng: e.latlng.lng });
      });
    }
  }
}
