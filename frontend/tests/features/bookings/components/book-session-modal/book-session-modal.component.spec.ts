import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import type { Mock } from 'vitest';
import { BookSessionModalComponent } from '../../../../../src/app/features/bookings/components/book-session-modal/book-session-modal.component';
import { TutorDetailsResponse } from '../../../../../src/app/core/models/tutor.models';
import { DEMO_STUDENT_ID } from '../../../../../src/app/core/models/session.models';

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
    httpMock.expectOne('/api/sessions').flush(body, { status, statusText: 'Error' });
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
    expect(text()).toContain('Subjects taught');
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
    const req = httpMock.expectOne('/api/sessions');
    expect(booked).not.toHaveBeenCalled();
    expect(req.request.headers.get('X-Student-Id')).toBe(DEMO_STUDENT_ID);
    expect(req.request.body).toEqual({ tutorId: 'tutor-1', startTime: SLOT.start, endTime: SLOT.end });
    req.flush({ id: 'session-1' });

    expect(booked).toHaveBeenCalledWith({ id: 'session-1' });
  });

  it('Given_RequestInFlight_When_ConfirmingAgain_Then_SendsOnlyOneRequest', () => {
    component.confirm();
    component.confirm();
    fixture.detectChanges();

    expect(confirmButton()?.disabled).toBe(true);
    httpMock.expectOne('/api/sessions').flush({ id: 'session-1' });
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

      httpMock.expectOne('/api/sessions').flush({ id: 'session-1' });
      expect(booked).toHaveBeenCalledWith({ id: 'session-1' });
    });

    it('Given_OpenModal_When_Destroyed_Then_ReturnsFocusToOpener', () => {
      fixture.destroy();

      expect(document.activeElement).toBe(opener);
    });
  });

  it('Given_FocusOnLastButton_When_PressingTab_Then_WrapsToFirstButton', () => {
    confirmButton()?.focus();

    const tab = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    dialog().dispatchEvent(tab);

    expect(tab.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(cancelButton());
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
