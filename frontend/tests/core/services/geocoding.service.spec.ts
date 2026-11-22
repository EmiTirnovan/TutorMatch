import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GeocodedPlace, GeocodingService, NOMINATIM_URL } from '../../../src/app/core/services/geocoding.service';

describe('GeocodingService', () => {
  let service: GeocodingService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(GeocodingService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('Given_Query_When_Searching_Then_LimitsToRomaniaAndMapsCoordinatesToNumbers', () => {
    let result: GeocodedPlace[] | undefined;
    service.search('Carol I 11, Iasi').subscribe(r => (result = r));

    const req = httpMock.expectOne(r => r.url === `${NOMINATIM_URL}/search`);
    expect(req.request.params.get('q')).toBe('Carol I 11, Iasi');
    expect(req.request.params.get('countrycodes')).toBe('ro');
    expect(req.request.params.get('format')).toBe('jsonv2');
    req.flush([{ display_name: 'Carol I 11, Iasi', lat: '47.17', lon: '27.57' }]);

    expect(result).toEqual([{ address: 'Carol I 11, Iasi', lat: 47.17, lng: 27.57 }]);
  });

  it('Given_KnownPoint_When_Reversing_Then_ReturnsPlace', () => {
    let result: GeocodedPlace | null | undefined;
    service.reverse(47.17, 27.57).subscribe(r => (result = r));

    const req = httpMock.expectOne(r => r.url === `${NOMINATIM_URL}/reverse`);
    expect(req.request.params.get('lat')).toBe('47.17');
    expect(req.request.params.get('lon')).toBe('27.57');
    req.flush({ display_name: 'Carol I 11, Iasi', lat: '47.17', lon: '27.57' });

    expect(result).toEqual({ address: 'Carol I 11, Iasi', lat: 47.17, lng: 27.57 });
  });

  it('Given_PointWithoutAddress_When_Reversing_Then_ReturnsNull', () => {
    let result: GeocodedPlace | null | undefined;
    service.reverse(43.5, 31).subscribe(r => (result = r));

    httpMock.expectOne(r => r.url === `${NOMINATIM_URL}/reverse`).flush({ error: 'Unable to geocode' });

    expect(result).toBeNull();
  });
});
