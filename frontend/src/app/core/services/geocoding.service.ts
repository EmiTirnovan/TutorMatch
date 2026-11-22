import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface GeocodedPlace {
  address: string;
  lat: number;
  lng: number;
}

interface NominatimPlace {
  lat: string;
  lon: string;
  display_name: string;
}

export const NOMINATIM_URL = 'https://nominatim.openstreetmap.org';

/**
 * Address lookup through OpenStreetMap Nominatim.
 * shortcut: public Nominatim allows ~1 request/second and no per-keystroke autocomplete,
 * so callers search on an explicit action; switch to a keyed provider (LocationIQ, Mapbox) for production traffic.
 */
@Injectable({ providedIn: 'root' })
export class GeocodingService {
  private readonly http = inject(HttpClient);

  search(query: string): Observable<GeocodedPlace[]> {
    const params = new HttpParams()
      .set('q', query)
      .set('format', 'jsonv2')
      .set('countrycodes', 'ro')
      .set('limit', 5)
      .set('accept-language', 'en');
    return this.http
      .get<NominatimPlace[]>(`${NOMINATIM_URL}/search`, { params })
      .pipe(map(places => places.map(toPlace)));
  }

  /** Returns null when the point is not on a known place (e.g. in the sea). */
  reverse(lat: number, lng: number): Observable<GeocodedPlace | null> {
    const params = new HttpParams()
      .set('lat', lat)
      .set('lon', lng)
      .set('format', 'jsonv2')
      .set('accept-language', 'en');
    return this.http
      .get<NominatimPlace | { error: string }>(`${NOMINATIM_URL}/reverse`, { params })
      .pipe(map(place => ('display_name' in place ? toPlace(place) : null)));
  }
}

function toPlace(place: NominatimPlace): GeocodedPlace {
  return { address: place.display_name, lat: Number(place.lat), lng: Number(place.lon) };
}
