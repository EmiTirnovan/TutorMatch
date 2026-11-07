import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import type { Mock } from 'vitest';
import { BookSessionModalComponent } from './book-session-modal.component';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { DEMO_STUDENT_ID } from '../../../../core/models/session.models';

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

  const text = (): string => fixture.nativeElement.textContent;
  const confirmButton = (): HTMLButtonElement | null => fixture.nativeElement.querySelector('.btn-accent');

  const submitAndFail = (status: number, body: object | null): void => {
    component.confirm();
    httpMock.expectOne('/api/sessions/book').flush(body, { status, statusText: 'Error' });
    fixture.detectChanges();
  };

  beforeEach(() => {
    // jsdom does not implement the native dialog API.
    HTMLDialogElement.prototype.showModal = vi.fn();
    HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
      this.dispatchEvent(new Event('close'));
    };
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(BookSessionModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('tutor', TUTOR);
    fixture.componentRef.setInput('slot', SLOT);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('Given_TutorAndSlot_When_Opened_Then_ShowsSummaryInModalDialog', () => {
    expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalled();
    expect(text()).toContain('Maria Dragomir');
    expect(text()).toContain('Discrete Math');
    expect(text()).toContain('75 RON / hour');
  });

  it('Given_TutorWithoutSubjects_When_Opened_Then_ShowsNotSpecified', () => {
    fixture.componentRef.setInput('tutor', { ...TUTOR, subjects: undefined });
    fixture.detectChanges();

    expect(text()).toContain('Not specified');
  });

  it('Given_SuccessfulResponse_When_Confirming_Then_EmitsBookedOnlyAfterResponse', () => {
    const booked = vi.fn();
    component.booked.subscribe(booked);

    confirmButton()?.click();
    const req = httpMock.expectOne('/api/sessions/book');
    expect(booked).not.toHaveBeenCalled();
    expect(req.request.body).toEqual({
      tutorId: 'tutor-1',
      studentId: DEMO_STUDENT_ID,
      startTime: SLOT.start,
      endTime: SLOT.end
    });
    req.flush({ id: 'session-1' });

    expect(booked).toHaveBeenCalledWith({ id: 'session-1' });
  });

  it('Given_RequestInFlight_When_ConfirmingAgain_Then_SendsOnlyOneRequest', () => {
    component.confirm();
    component.confirm();
    fixture.detectChanges();

    expect(confirmButton()?.disabled).toBe(true);
    httpMock.expectOne('/api/sessions/book').flush({ id: 'session-1' });
  });

  describe('closing', () => {
    let closed: Mock<() => void>;

    const dialog = (): HTMLDialogElement => fixture.nativeElement.querySelector('dialog');
    const pressEsc = (): boolean => dialog().dispatchEvent(new Event('cancel', { cancelable: true }));

    beforeEach(() => {
      closed = vi.fn<() => void>();
      component.closed.subscribe(closed);
    });

    it('Given_NoRequest_When_PressingEsc_Then_AllowsClosing', () => {
      expect(pressEsc()).toBe(true);
    });

    it('Given_NoRequest_When_ClickingCancel_Then_ClosesNativelyAndEmitsClosed', () => {
      const closeSpy = vi.spyOn(HTMLDialogElement.prototype, 'close');

      fixture.nativeElement.querySelector('.btn-secondary').click();

      expect(closeSpy).toHaveBeenCalledOnce();
      expect(closed).toHaveBeenCalledOnce();
    });

    it('Given_RequestInFlight_When_PressingEsc_Then_StaysOpenAndStillReportsBooked', () => {
      const booked = vi.fn();
      component.booked.subscribe(booked);
      component.confirm();

      expect(pressEsc()).toBe(false);
      expect(closed).not.toHaveBeenCalled();

      httpMock.expectOne('/api/sessions/book').flush({ id: 'session-1' });
      expect(booked).toHaveBeenCalledWith({ id: 'session-1' });
    });

    it('Given_RequestInFlight_When_BrowserForcesClose_Then_ReopensDialog', () => {
      component.confirm();
      vi.mocked(HTMLDialogElement.prototype.showModal).mockClear();

      dialog().dispatchEvent(new Event('close'));

      expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalledOnce();
      expect(closed).not.toHaveBeenCalled();
      httpMock.expectOne('/api/sessions/book').flush({ id: 'session-1' });
    });
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
});
