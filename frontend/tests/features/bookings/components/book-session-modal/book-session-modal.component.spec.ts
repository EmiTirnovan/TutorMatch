import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { NOMINATIM_URL } from '../../../../../src/app/core/services/geocoding.service';
import type { Mock } from 'vitest';
import { By } from '@angular/platform-browser';
import { LocationPickerMapComponent } from '../../../../../src/app/features/locations/components/location-picker-map/location-picker-map.component';
import { BookSessionModalComponent } from '../../../../../src/app/features/bookings/components/book-session-modal/book-session-modal.component';
import { TutorDetailsResponse } from '../../../../../src/app/core/models/tutor.models';
import { DEMO_STUDENT_ID, SessionType } from '../../../../../src/app/core/models/session.models';

const TUTOR: TutorDetailsResponse = {
  id: 'tutor-1',
  name: 'Maria Dragomir',
  email: 'maria@test.ro',
  bio: 'Bio',
  hourlyRate: 75,
  subjects: ['Discrete Math'],
  availability: []
};
const SLOT = { start: '2026-11-06T10:00:00.000Z', end: '2026-11-06T11:00:00.000Z' };

describe('BookSessionModalComponent', () => {
  let fixture: ComponentFixture<BookSessionModalComponent>;
  let httpMock: HttpTestingController;
  let component: BookSessionModalComponent;
  let opener: HTMLButtonElement;

  const text = (): string => fixture.nativeElement.textContent;
  const dialog = (): HTMLElement => fixture.nativeElement.querySelector('[role="dialog"]');
  const confirmButton = (): HTMLButtonElement | null => fixture.nativeElement.querySelector('.btn-accent');
  const cancelButton = (): HTMLButtonElement => fixture.nativeElement.querySelector('.btn-secondary');

  const submitAndFail = (status: number, body: object | null): void => {
    component.confirm();
    httpMock.expectOne('/api/v2/sessions').flush(body, { status, statusText: 'Error' });
    fixture.detectChanges();
  };

  beforeEach(() => {
    opener = document.createElement('button');
    document.body.appendChild(opener);
    opener.focus();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(BookSessionModalComponent);
    component = fixture.componentInstance;
    document.body.appendChild(fixture.nativeElement);
    fixture.componentRef.setInput('tutor', TUTOR);
    fixture.componentRef.setInput('slot', SLOT);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
    fixture.nativeElement.remove();
    opener.remove();
  });

  it('Given_TutorAndSlot_When_Opened_Then_ShowsSummaryAndFocusesDialog', () => {
    expect(document.activeElement).toBe(dialog());
    expect(dialog().getAttribute('aria-modal')).toBe('true');
    expect(text()).toContain('Maria Dragomir');
    expect(text()).toContain('Subject / Topic to study');
    expect(text()).toContain('Discrete Math');
    expect(text()).toContain('75 RON / hour');
  });

  it('Given_TutorWithoutSubjects_When_Opened_Then_DefaultsToCustomSubjectInput', () => {
    fixture.componentRef.setInput('tutor', { ...TUTOR, subjects: undefined });
    component.ngOnInit();
    fixture.detectChanges();

    expect(component.selectedSubjectType()).toBe('__custom__');
    expect(fixture.nativeElement.querySelector('#booking-custom-subject')).not.toBeNull();
  });

  it('Given_SuccessfulResponse_When_Confirming_Then_EmitsBookedOnlyAfterResponse', () => {
    const booked = vi.fn();
    component.booked.subscribe(booked);

    confirmButton()?.click();
    const req = httpMock.expectOne('/api/v2/sessions');
    expect(booked).not.toHaveBeenCalled();
    expect(req.request.headers.get('X-Student-Id')).toBe(DEMO_STUDENT_ID);
    expect(req.request.body).toEqual({
      tutorId: 'tutor-1',
      startTime: SLOT.start,
      endTime: SLOT.end,
      subject: 'Discrete Math',
      type: SessionType.Online
    });
    req.flush({ id: 'session-1', subject: 'Discrete Math' });

    expect(booked).toHaveBeenCalledWith({ id: 'session-1', subject: 'Discrete Math' });
  });

  it('Given_CustomSubjectAndNotes_When_Confirming_Then_SendsCustomSubjectAndNotes', () => {
    component.selectedSubjectType.set('__custom__');
    component.customSubject.set('Teoria Grafurilor');
    component.notes.set('Ajutor la tema 2 si arbori minimi');
    fixture.detectChanges();

    confirmButton()?.click();
    const req = httpMock.expectOne('/api/v2/sessions');
    expect(req.request.body).toEqual({
      tutorId: 'tutor-1',
      startTime: SLOT.start,
      endTime: SLOT.end,
      subject: 'Teoria Grafurilor',
      notes: 'Ajutor la tema 2 si arbori minimi',
      type: SessionType.Online
    });
    req.flush({ id: 'session-2', subject: 'Teoria Grafurilor', notes: 'Ajutor la tema 2 si arbori minimi' });
  });

  it('Given_RequestInFlight_When_ConfirmingAgain_Then_SendsOnlyOneRequest', () => {
    component.confirm();
    component.confirm();
    fixture.detectChanges();

    expect(confirmButton()?.disabled).toBe(true);
    httpMock.expectOne('/api/v2/sessions').flush({ id: 'session-1' });
  });

  describe('closing', () => {
    let closed: Mock<() => void>;

    const pressEsc = (): void => {
      dialog().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    };

    beforeEach(() => {
      closed = vi.fn<() => void>();
      component.closed.subscribe(closed);
    });

    it('Given_NoRequest_When_PressingEsc_Then_EmitsClosed', () => {
      pressEsc();

      expect(closed).toHaveBeenCalledOnce();
    });

    it('Given_NoRequest_When_ClickingBackdrop_Then_EmitsClosed', () => {
      fixture.nativeElement.querySelector('.modal-backdrop').click();

      expect(closed).toHaveBeenCalledOnce();
    });

    it('Given_NoRequest_When_ClickingInsideDialog_Then_StaysOpen', () => {
      dialog().click();

      expect(closed).not.toHaveBeenCalled();
    });

    it('Given_NoRequest_When_ClickingCancel_Then_EmitsClosed', () => {
      cancelButton().click();

      expect(closed).toHaveBeenCalledOnce();
    });

    it('Given_RequestInFlight_When_PressingEscOrClickingBackdrop_Then_StaysOpenAndStillReportsBooked', () => {
      const booked = vi.fn();
      component.booked.subscribe(booked);
      component.confirm();

      pressEsc();
      fixture.nativeElement.querySelector('.modal-backdrop').click();
      expect(closed).not.toHaveBeenCalled();

      httpMock.expectOne('/api/v2/sessions').flush({ id: 'session-1' });
      expect(booked).toHaveBeenCalledWith({ id: 'session-1' });
    });

    it('Given_OpenModal_When_Destroyed_Then_ReturnsFocusToOpener', () => {
      fixture.destroy();

      expect(document.activeElement).toBe(opener);
    });
  });

  it('Given_FocusOnLastButton_When_PressingTab_Then_WrapsToFirstFocusableElement', () => {
    confirmButton()?.focus();

    const tab = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    dialog().dispatchEvent(tab);

    expect(tab.defaultPrevented).toBe(true);
    const firstFocusable = dialog().querySelector('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled])');
    expect(document.activeElement).toBe(firstFocusable);
  });

  it('Given_409_When_Confirming_Then_EmitsConflict', () => {
    const conflict = vi.fn();
    component.conflict.subscribe(conflict);

    submitAndFail(409, { code: 'Session.Conflict', description: 'taken' });

    expect(conflict).toHaveBeenCalledOnce();
  });

  it('Given_DailyLimit400_When_Confirming_Then_ShowsLimitAndHidesConfirm', () => {
    const conflict = vi.fn();
    component.conflict.subscribe(conflict);

    submitAndFail(400, { code: 'Session.DailyLimit', description: 'limit' });

    expect(text()).toContain('You already have 2 sessions on this date');
    expect(confirmButton()).toBeNull();
    expect(conflict).not.toHaveBeenCalled();
  });

  it('Given_Other400_When_Confirming_Then_TellsStudentToChooseAnotherTime', () => {
    submitAndFail(400, { code: 'Session.Validation', description: 'internal detail' });

    expect(text()).toContain('choose another time');
    expect(text()).not.toContain('internal detail');
  });

  it('Given_ServerError_When_Confirming_Then_ShowsErrorAndAllowsRetry', () => {
    submitAndFail(500, null);

    expect(text()).toContain('Please try again');
    expect(confirmButton()?.disabled).toBe(false);
  });

  describe('session format', () => {
    const query = (selector: string): HTMLElement | null => fixture.nativeElement.querySelector(selector);
    const choosePhysical = (): void => {
      (query('[data-testid="type-physical"]') as HTMLInputElement).click();
      fixture.detectChanges();
    };
    const picker = (): LocationPickerMapComponent =>
      fixture.debugElement.query(By.directive(LocationPickerMapComponent)).componentInstance;
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
    const CORP_C = { display_name: 'Corp C, Bulevardul Carol I 11, Iasi', lat: '47.1745', lon: '27.5718' };

    it('Given_DefaultFormat_When_Opened_Then_OnlineSelectedWithMeetInfo', () => {
      expect((query('[data-testid="type-online"]') as HTMLInputElement).checked).toBe(true);
      expect(query('[data-testid="online-info"]')?.textContent).toContain('Google Meet link will be generated automatically');
      expect(query('app-location-picker-map')).toBeNull();
    });

    it('Given_PhysicalSelected_When_Rendering_Then_ShowsLocationPickerAndDetailsField', () => {
      choosePhysical();

      expect(component.sessionType()).toBe(SessionType.Physical);
      expect(query('[data-testid="online-info"]')).toBeNull();
      expect(query('app-location-picker-map')).not.toBeNull();
      expect(query('#booking-location-details')).not.toBeNull();
    });

    it('Given_TypedButUnverifiedAddress_When_Confirming_Then_ShowsErrorAndSendsNothing', () => {
      choosePhysical();
      typeAddress('asdf bla bla');

      confirmButton()?.click();
      fixture.detectChanges();

      httpMock.expectNone('/api/v2/sessions');
      expect(text()).toContain('Search the address and choose it, or pin the spot on the map');
      expect(query('#booking-location')?.getAttribute('aria-invalid')).toBe('true');
    });

    it('Given_ApartmentDetailsInAddress_When_Searching_Then_AppendsThemToTheDetailsField', () => {
      choosePhysical();
      component.locationDetails.set('interfon 18');
      typeAddress('Carol I 11, sc. B, et. 4, ap. 18, Iasi');

      search().flush([CORP_C]);

      expect(component.locationDetails()).toBe('interfon 18, sc. B, et. 4, ap. 18');
    });

    it('Given_PickedLocationAndRoom_When_Confirming_Then_BooksWithAddressCoordinatesAndRoom', () => {
      choosePhysical();
      typeAddress('Carol I 11 Iasi');
      component.locationDetails.set('sala C309');
      search().flush([CORP_C]);
      fixture.detectChanges();

      confirmButton()?.click();
      const booking = httpMock.expectOne('/api/v2/sessions');
      expect(booking.request.body).toEqual({
        tutorId: 'tutor-1',
        startTime: SLOT.start,
        endTime: SLOT.end,
        subject: 'Discrete Math',
        type: SessionType.Physical,
        locationName: 'Corp C, Bulevardul Carol I 11, Iasi - sala C309',
        latitude: 47.1745,
        longitude: 27.5718
      });
      booking.flush({ id: 'session-3' });
    });

    it('Given_VerifiedAddressThenEdited_When_Confirming_Then_SendsNothing', () => {
      choosePhysical();
      typeAddress('Carol I 11 Iasi');
      search().flush([CORP_C]);
      fixture.detectChanges();
      typeAddress('Corp C, Carol I 11, Iasi, langa cantina');

      confirmButton()?.click();

      httpMock.expectNone('/api/v2/sessions');
      expect(component.pickedLocation()).toBeNull();
    });

    describe('map click', () => {
      beforeEach(() => vi.useFakeTimers());
      afterEach(() => vi.useRealTimers());

      it('Given_LookupPending_When_Confirming_Then_SendsNothing', () => {
        choosePhysical();
        picker().onMapPicked({ lat: 47.1745, lng: 27.5718 });

        component.confirm();

        httpMock.expectNone('/api/v2/sessions');
        vi.advanceTimersByTime(1000);
        httpMock.expectOne(req => req.url === `${NOMINATIM_URL}/reverse`).flush(CORP_C);
        expect(component.pickedLocation()).toEqual({ lat: 47.1745, lng: 27.5718, address: 'Corp C, Bulevardul Carol I 11, Iasi' });
      });

      it('Given_LookupPending_When_SwitchingToOnline_Then_CanBookOnlineRightAway', () => {
        choosePhysical();
        picker().onMapPicked({ lat: 47.1745, lng: 27.5718 });

        (query('[data-testid="type-online"]') as HTMLInputElement).click();
        fixture.detectChanges();
        vi.advanceTimersByTime(1000);

        httpMock.expectNone(req => req.url.startsWith(NOMINATIM_URL));
        expect(component.pickedLocation()).toBeNull();
        confirmButton()?.click();
        httpMock.expectOne('/api/v2/sessions').flush({ id: 'session-5' });
      });
    });
  });
});
