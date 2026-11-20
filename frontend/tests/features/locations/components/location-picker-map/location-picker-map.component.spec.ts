import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import {
  LocationPickerMapComponent,
  PickedLocation
} from '../../../../../src/app/features/locations/components/location-picker-map/location-picker-map.component';
import { NOMINATIM_URL } from '../../../../../src/app/core/services/geocoding.service';

describe('LocationPickerMapComponent', () => {
  let fixture: ComponentFixture<LocationPickerMapComponent>;
  let component: LocationPickerMapComponent;
  let httpMock: HttpTestingController;
  let emitted: (PickedLocation | null)[];
  let busy: boolean[];
  let details: string[];

  const text = (): string => fixture.nativeElement.textContent;
  const query = (selector: string): HTMLElement | null => fixture.nativeElement.querySelector(selector);
  const lastLocation = (): PickedLocation | null | undefined => emitted.at(-1);
  const typeAddress = (value: string): void => {
    const input = query('#booking-location') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  const search = (): TestRequest => {
    (query('[data-testid="search-address-btn"]') as HTMLButtonElement).click();
    return httpMock.expectOne(req => req.url === `${NOMINATIM_URL}/search`);
  };
  const place = (name: string, lat: string, lon: string) => ({ display_name: name, lat, lon });
  const CORP_C = place('Corp C, Bulevardul Carol I 11, Iasi', '47.1745', '27.5718');

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(LocationPickerMapComponent);
    component = fixture.componentInstance;
    emitted = [];
    busy = [];
    details = [];
    component.locationChange.subscribe(location => emitted.push(location));
    component.busyChange.subscribe(value => busy.push(value));
    component.unitDetailsFound.subscribe(value => details.push(value));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('Given_NewPicker_When_Rendered_Then_ShowsAddressSearchAndInteractiveMap', () => {
    expect(query('#booking-location')).not.toBeNull();
    expect(query('[data-testid="search-address-btn"]')).not.toBeNull();
    expect(query('app-location-map')).not.toBeNull();
  });

  it('Given_ParentMarksMissing_When_Rendering_Then_ShowsErrorOnAddressField', () => {
    component.markMissing();
    fixture.detectChanges();

    expect(text()).toContain('Search the address and choose it, or pin the spot on the map');
    expect(query('#booking-location')?.getAttribute('aria-invalid')).toBe('true');
  });

  it('Given_TooShortQuery_When_Searching_Then_AsksForMoreAndCallsNothing', () => {
    typeAddress('ab');

    (query('[data-testid="search-address-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock.expectNone(req => req.url.startsWith(NOMINATIM_URL));
    expect(text()).toContain('Type the street and number');
  });

  it('Given_SingleMatch_When_Searching_Then_EmitsCoordinatesAndAddress', () => {
    typeAddress('Carol I 11 Iasi');

    const req = search();
    expect(req.request.params.get('q')).toBe('Carol I 11 Iasi');
    expect(req.request.params.get('countrycodes')).toBe('ro');
    expect(busy).toEqual([true]);
    req.flush([CORP_C]);
    fixture.detectChanges();

    expect(lastLocation()).toEqual({ lat: 47.1745, lng: 27.5718, address: 'Corp C, Bulevardul Carol I 11, Iasi' });
    expect(busy).toEqual([true, false]);
    expect(query('[data-testid="location-verified"]')).not.toBeNull();
  });

  it('Given_ApartmentDetailsInAddress_When_Searching_Then_EmitsThemAndSearchesTheBuilding', () => {
    typeAddress('Carol I 11, sc. B, et. 4, ap. 18, Iasi');

    const req = search();

    expect(req.request.params.get('q')).toBe('Carol I 11, Iasi');
    expect(component.address()).toBe('Carol I 11, Iasi');
    expect(details).toEqual(['sc. B, et. 4, ap. 18']);
    req.flush([CORP_C]);
  });

  it('Given_SeveralMatches_When_ChoosingOne_Then_EmitsTheChosenPlace', () => {
    typeAddress('Carol I');

    search().flush([CORP_C, place('Carol I, Constanta', '44.18', '28.63')]);
    fixture.detectChanges();
    const options = fixture.nativeElement.querySelectorAll('.address-result') as NodeListOf<HTMLButtonElement>;
    expect(options.length).toBe(2);
    expect(component.pickedPoint()).toBeNull();

    options[1].click();
    fixture.detectChanges();

    expect(lastLocation()).toEqual({ lat: 44.18, lng: 28.63, address: 'Carol I, Constanta' });
    expect(query('[data-testid="address-results"]')).toBeNull();
  });

  it('Given_VerifiedAddress_When_EditingText_Then_EmitsNullUntilSearchedAgain', () => {
    typeAddress('Carol I 11 Iasi');
    search().flush([CORP_C]);
    fixture.detectChanges();

    typeAddress('Corp C, Carol I 11, Iasi, langa cantina');

    expect(lastLocation()).toBeNull();
    expect(component.pickedPoint()).toBeNull();
  });

  it('Given_SearchFails_When_Searching_Then_ShowsRetryMessage', () => {
    typeAddress('Carol I 11 Iasi');

    search().flush(null, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();

    expect(text()).toContain('could not search the address');
    expect(busy.at(-1)).toBe(false);
  });

  describe('fallback searches', () => {
    beforeEach(() => vi.useFakeTimers());
    afterEach(() => vi.useRealTimers());

    const nextSearch = (): TestRequest => {
      vi.advanceTimersByTime(1000);
      return httpMock.expectOne(req => req.url === `${NOMINATIM_URL}/search`);
    };

    it('Given_UnknownAddress_When_AllAttemptsFail_Then_ShowsNotFoundAndNoPin', () => {
      typeAddress('Strada Inexistenta 999');

      search().flush([]);
      expect(component.isGeocoding()).toBe(true);
      const street = nextSearch();
      expect(street.request.params.get('q')).toBe('Strada Inexistenta');
      street.flush([]);
      fixture.detectChanges();

      expect(text()).toContain('Address not found');
      expect(component.pickedPoint()).toBeNull();
      expect(component.isGeocoding()).toBe(false);
    });

    it('Given_BlockNotInMap_When_Searching_Then_RetriesWithoutBlockWithoutStreetHint', () => {
      typeAddress('Str. Pacurari 5, bl. A2, Iasi');

      search().flush([]);
      expect(httpMock.match(req => req.url.startsWith(NOMINATIM_URL))).toEqual([]);
      const withoutBlock = nextSearch();
      expect(withoutBlock.request.params.get('q')).toBe('Str. Pacurari 5, Iasi');
      withoutBlock.flush([place('Strada Pacurari 5, Iasi', '47.17', '27.56')]);
      fixture.detectChanges();

      expect(component.pickedPoint()).toEqual({ lat: 47.17, lng: 27.56 });
      expect(query('[data-testid="location-hint"]')).toBeNull();
    });

    it('Given_HouseNumberNotInMap_When_StreetFound_Then_PinsStreetAndAsksToMovePin', () => {
      typeAddress('Strada Pacurari 999, Iasi');

      search().flush([]);
      nextSearch().flush([place('Strada Pacurari, Iasi', '47.17', '27.56')]);
      fixture.detectChanges();

      expect(component.pickedPoint()).toEqual({ lat: 47.17, lng: 27.56 });
      expect(query('[data-testid="location-hint"]')?.textContent).toContain('not the exact number');
    });

    it('Given_PickerDestroyed_When_RetryWasPending_Then_SendsNoMoreLookups', () => {
      typeAddress('Strada Inexistenta 999');
      search().flush([]);

      fixture.destroy();
      vi.advanceTimersByTime(1000);

      httpMock.expectNone(req => req.url.startsWith(NOMINATIM_URL));
    });
  });

  describe('map click', () => {
    beforeEach(() => vi.useFakeTimers());
    afterEach(() => vi.useRealTimers());

    const reverse = (): TestRequest => httpMock.expectOne(req => req.url === `${NOMINATIM_URL}/reverse`);

    it('Given_ClickOnKnownPlace_When_Debounced_Then_FillsAddressAndEmitsLocation', () => {
      component.onMapPicked({ lat: 47.1745, lng: 27.5718 });
      expect(component.isGeocoding()).toBe(true);
      vi.advanceTimersByTime(1000);
      const req = reverse();
      expect(req.request.params.get('lat')).toBe('47.1745');
      req.flush(CORP_C);
      fixture.detectChanges();

      expect(component.address()).toBe('Corp C, Bulevardul Carol I 11, Iasi');
      expect(lastLocation()).toEqual({ lat: 47.1745, lng: 27.5718, address: 'Corp C, Bulevardul Carol I 11, Iasi' });
      expect(component.isGeocoding()).toBe(false);
    });

    it('Given_RapidClicks_When_Debounced_Then_SendsOnlyOneLookupForTheLastClick', () => {
      component.onMapPicked({ lat: 1, lng: 1 });
      vi.advanceTimersByTime(500);
      component.onMapPicked({ lat: 47.1745, lng: 27.5718 });
      vi.advanceTimersByTime(1000);

      const req = reverse();
      expect(req.request.params.get('lat')).toBe('47.1745');
      req.flush(CORP_C);
    });

    it('Given_ClickOnNoAddress_When_Debounced_Then_ShowsErrorAndNoPin', () => {
      component.onMapPicked({ lat: 43.5, lng: 31.0 });
      vi.advanceTimersByTime(1000);
      reverse().flush({ error: 'Unable to geocode' });
      fixture.detectChanges();

      expect(component.pickedPoint()).toBeNull();
      expect(lastLocation()).toBeNull();
      expect(text()).toContain('no known address at that point');
    });
  });
});
