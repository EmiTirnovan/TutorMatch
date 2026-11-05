import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { RateSessionModalComponent } from './rate-session-modal.component';
import { TutorService } from '../../../../core/services/tutor.service';

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
});
