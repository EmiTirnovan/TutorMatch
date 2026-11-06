import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, of, throwError } from 'rxjs';
import { RateSessionModalComponent } from '../../../../../src/app/features/tutors/components/rate-session-modal/rate-session-modal.component';
import { TutorService } from '../../../../../src/app/core/services/tutor.service';

describe('RateSessionModalComponent', () => {
  let component: RateSessionModalComponent;
  let fixture: ComponentFixture<RateSessionModalComponent>;
  let rateSpy: ReturnType<typeof vi.fn<TutorService['rateSession']>>;

  const createModal = (sessionStatus: string, response: Observable<void> = of(undefined)): void => {
    rateSpy = vi.fn<TutorService['rateSession']>().mockReturnValue(response);
    TestBed.configureTestingModule({
      imports: [RateSessionModalComponent],
      providers: [{ provide: TutorService, useValue: { rateSession: rateSpy } }]
    });

    fixture = TestBed.createComponent(RateSessionModalComponent);
    fixture.componentRef.setInput('tutorId', 'tutor-1');
    fixture.componentRef.setInput('sessionId', 'session-1');
    fixture.componentRef.setInput('studentId', 'student-1');
    fixture.componentRef.setInput('sessionStatus', sessionStatus);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  it('Given_CompletedSession_When_SubmittingValidRating_Then_PostsTrimmedRatingAndEmitsRated', () => {
    createModal('Completed');
    const ratedEvents: unknown[] = [];
    component.rated.subscribe((request) => ratedEvents.push(request));

    component.selectScore(4);
    component.form.controls.notes.setValue('  Very clear  ');
    component.submit();

    expect(rateSpy).toHaveBeenCalledWith('tutor-1', 'session-1', 'student-1', { score: 4, notes: 'Very clear' });
    expect(ratedEvents).toEqual([{ score: 4, notes: 'Very clear' }]);
    expect(component.isRated()).toBe(true);
  });

  it('Given_NoComment_When_Submitting_Then_OmitsNotes', () => {
    createModal('Completed');

    component.selectScore(5);
    component.submit();

    expect(rateSpy).toHaveBeenCalledWith('tutor-1', 'session-1', 'student-1', { score: 5 });
  });

  it('Given_NoScoreSelected_When_Submitting_Then_DoesNotCallApi', () => {
    createModal('Completed');

    component.submit();

    expect(rateSpy).not.toHaveBeenCalled();
    expect(component.form.controls.score.touched).toBe(true);
  });

  it('Given_ScoreOutsideRange_When_Validating_Then_FormIsInvalid', () => {
    createModal('Completed');

    for (const score of [0, 6, -1]) {
      component.form.controls.score.setValue(score);
      component.submit();
      expect(component.form.controls.score.invalid).toBe(true);
    }
    expect(rateSpy).not.toHaveBeenCalled();
  });

  it('Given_AlreadySubmittedRating_When_SubmittingAgain_Then_DoesNotCallApiTwice', () => {
    createModal('Completed');

    component.selectScore(3);
    component.submit();
    component.submit();

    expect(rateSpy).toHaveBeenCalledTimes(1);
  });

  it('Given_ServerReportsDuplicate_When_Submitting_Then_ShowsErrorAndLocksForm', () => {
    const conflict = new HttpErrorResponse({ status: 409, error: { detail: 'Această sesiune a fost deja evaluată.' } });
    createModal('Completed', throwError(() => conflict));

    component.selectScore(2);
    component.submit();

    expect(component.errorMessage()).toBe('Această sesiune a fost deja evaluată.');
    expect(component.isRated()).toBe(true);
    component.submit();
    expect(rateSpy).toHaveBeenCalledTimes(1);
  });

  it('Given_ServerError_When_Submitting_Then_AllowsRetry', () => {
    createModal('Completed', throwError(() => new HttpErrorResponse({ status: 500 })));

    component.selectScore(4);
    component.submit();

    expect(component.errorMessage()).toBeTruthy();
    expect(component.isRated()).toBe(false);
    expect(component.isSubmitting()).toBe(false);
  });

  it('Given_NonCompletedSession_When_Submitting_Then_BlocksRatingAndHidesForm', () => {
    createModal('Booked');

    component.selectScore(5);
    component.submit();

    expect(rateSpy).not.toHaveBeenCalled();
    expect(component.canRate()).toBe(false);
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('form')).toBeNull();
    expect(element.textContent).toContain('only after a session is completed');
  });

  it('Given_OpenModal_When_PressingEscape_Then_EmitsClosed', () => {
    createModal('Completed');
    let closedCount = 0;
    component.closed.subscribe(() => closedCount++);

    const dialog = (fixture.nativeElement as HTMLElement).querySelector('[role="dialog"]') as HTMLElement;
    dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(closedCount).toBe(1);
  });

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const query = <T extends HTMLElement>(selector: string): T => element().querySelector(selector) as T;
  const stars = (): HTMLButtonElement[] => Array.from(element().querySelectorAll<HTMLButtonElement>('button[role="radio"]'));
  const submitForm = (): void => {
    query<HTMLFormElement>('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('Given_OpenModal_When_Rendering_Then_ExposesAccessibleDialogWithTitle', () => {
    createModal('Completed');

    const dialog = query('[role="dialog"]');
    const titleId = dialog.getAttribute('aria-labelledby') as string;
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(element().querySelector(`#${titleId}`)?.textContent).toContain('Rate your session');
  });

  it('Given_RenderedModal_When_AfterViewInit_Then_FocusesDialog', () => {
    createModal('Completed');
    document.body.appendChild(element());

    component.ngAfterViewInit();

    expect(document.activeElement).toBe(query('[role="dialog"]'));
    element().remove();
  });

  it('Given_OpenModal_When_ClickingBackdrop_Then_EmitsClosed', () => {
    createModal('Completed');
    let closedCount = 0;
    component.closed.subscribe(() => closedCount++);

    query('.modal-backdrop').click();

    expect(closedCount).toBe(1);
  });

  it('Given_OpenModal_When_ClickingInsideDialog_Then_DoesNotClose', () => {
    createModal('Completed');
    let closedCount = 0;
    component.closed.subscribe(() => closedCount++);

    query('[role="dialog"]').click();
    query('h2').click();

    expect(closedCount).toBe(0);
  });

  it('Given_UnratedSession_When_ClickingCancel_Then_EmitsClosedAndLabelsButtonCancel', () => {
    createModal('Completed');
    let closedCount = 0;
    component.closed.subscribe(() => closedCount++);
    const cancel = query<HTMLButtonElement>('.modal-actions .btn-secondary');

    cancel.click();

    expect(cancel.textContent?.trim()).toBe('Cancel');
    expect(closedCount).toBe(1);
  });

  it('Given_NonCompletedSession_When_ClickingClose_Then_EmitsClosedAndShowsNoStars', () => {
    createModal('Booked');
    let closedCount = 0;
    component.closed.subscribe(() => closedCount++);

    query<HTMLButtonElement>('.modal-actions .btn-secondary').click();

    expect(closedCount).toBe(1);
    expect(stars().length).toBe(0);
    expect(query('.alert-warning').getAttribute('role')).toBe('alert');
  });

  it('Given_CompletedSession_When_Rendering_Then_ShowsFiveLabelledUncheckedStarsInRadioGroup', () => {
    createModal('Completed');

    expect(query('.star-input').getAttribute('role')).toBe('radiogroup');
    expect(stars().map((star) => star.getAttribute('aria-label'))).toEqual([
      '1 star',
      '2 stars',
      '3 stars',
      '4 stars',
      '5 stars'
    ]);
    expect(stars().map((star) => star.getAttribute('aria-checked'))).toEqual(['false', 'false', 'false', 'false', 'false']);
    expect(query('.alert-success')).toBeNull();
  });

  it('Given_Stars_When_ClickingThirdStar_Then_ChecksItAndFillsThreeStars', () => {
    createModal('Completed');

    stars()[2].click();
    fixture.detectChanges();

    expect(stars()[2].getAttribute('aria-checked')).toBe('true');
    expect(stars()[3].getAttribute('aria-checked')).toBe('false');
    expect(element().querySelectorAll('.star-filled').length).toBe(3);
  });

  it('Given_FormWithoutScore_When_SubmittingFromDom_Then_ShowsScoreErrorAndDoesNotCallApi', () => {
    createModal('Completed');

    submitForm();

    expect(query('.error-feedback').textContent).toContain('Choose a rating from 1 to 5 stars.');
    expect(rateSpy).not.toHaveBeenCalled();
  });

  it('Given_ScoreSelected_When_ErrorFeedbackWasShown_Then_ErrorDisappears', () => {
    createModal('Completed');
    submitForm();

    stars()[4].click();
    fixture.detectChanges();

    expect(query('.error-feedback')).toBeNull();
  });

  it('Given_ScoreAndComment_When_SubmittingFromDom_Then_ShowsThankYouAndLocksForm', () => {
    createModal('Completed');
    stars()[4].click();
    const notes = query<HTMLTextAreaElement>('textarea');
    notes.value = 'Great session';
    notes.dispatchEvent(new Event('input'));

    submitForm();

    expect(rateSpy).toHaveBeenCalledWith('tutor-1', 'session-1', 'student-1', { score: 5, notes: 'Great session' });
    expect(query('.alert-success').textContent).toContain('Thank you. Your rating was saved.');
    expect(query('.alert-success').getAttribute('role')).toBe('status');
    expect(query('.alert-danger')).toBeNull();
    expect(query<HTMLButtonElement>('button[type="submit"]').disabled).toBe(true);
    expect(stars().every((star) => star.disabled)).toBe(true);
    expect(notes.readOnly).toBe(true);
    expect(query<HTMLButtonElement>('.modal-actions .btn-secondary').textContent?.trim()).toBe('Close');
  });

  it('Given_RatedSession_When_ClickingAnotherStar_Then_ScoreDoesNotChange', () => {
    createModal('Completed');
    component.selectScore(2);
    component.submit();

    component.selectScore(5);

    expect(component.form.controls.score.value).toBe(2);
  });

  it('Given_PendingRequest_When_Submitting_Then_ShowsSubmittingLabelAndDisablesSubmit', () => {
    createModal('Completed', new Subject<void>());
    stars()[3].click();

    submitForm();

    const submit = query<HTMLButtonElement>('button[type="submit"]');
    expect(submit.textContent?.trim()).toBe('Submitting...');
    expect(submit.disabled).toBe(true);
    expect(component.isRated()).toBe(false);
  });

  it('Given_PendingRequest_When_SubmittingAgain_Then_DoesNotCallApiTwice', () => {
    createModal('Completed', new Subject<void>());
    component.selectScore(3);

    component.submit();
    component.submit();

    expect(rateSpy).toHaveBeenCalledTimes(1);
  });

  it('Given_WhitespaceOnlyComment_When_Submitting_Then_OmitsNotes', () => {
    createModal('Completed');
    component.selectScore(4);
    component.form.controls.notes.setValue('    ');

    component.submit();

    expect(rateSpy).toHaveBeenCalledWith('tutor-1', 'session-1', 'student-1', { score: 4 });
  });

  it('Given_ServerReportsDuplicate_When_SubmittingFromDom_Then_ShowsServerMessageWithoutSuccessAlert', () => {
    const conflict = new HttpErrorResponse({ status: 409, error: { detail: 'Această sesiune a fost deja evaluată.' } });
    createModal('Completed', throwError(() => conflict));
    stars()[1].click();

    submitForm();

    expect(query('.alert-danger').textContent).toContain('Această sesiune a fost deja evaluată.');
    expect(query('.alert-success')).toBeNull();
    expect(query<HTMLButtonElement>('button[type="submit"]').disabled).toBe(true);
    expect(stars().every((star) => star.disabled)).toBe(true);
  });

  it('Given_ServerErrorWithoutDetail_When_SubmittingFromDom_Then_ShowsGenericMessageAndKeepsFormEditable', () => {
    createModal('Completed', throwError(() => new HttpErrorResponse({ status: 500 })));
    stars()[1].click();

    submitForm();

    expect(query('.alert-danger').textContent).toContain('The rating could not be saved. Please try again.');
    expect(query<HTMLButtonElement>('button[type="submit"]').disabled).toBe(false);
    expect(stars().some((star) => star.disabled)).toBe(false);
  });

  it('Given_FailedAttempt_When_ResubmittingSuccessfully_Then_ClearsErrorAndShowsThankYou', () => {
    createModal('Completed', throwError(() => new HttpErrorResponse({ status: 500 })));
    stars()[2].click();
    submitForm();
    rateSpy.mockReturnValue(of(undefined));

    submitForm();

    expect(rateSpy).toHaveBeenCalledTimes(2);
    expect(query('.alert-danger')).toBeNull();
    expect(query('.alert-success')).not.toBeNull();
  });
});
