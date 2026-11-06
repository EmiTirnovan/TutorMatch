import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TutorFilterBarComponent } from '../../../../../src/app/features/tutors/components/tutor-filter-bar/tutor-filter-bar.component';
import { DEFAULT_FILTER_CRITERIA, TutorFilterCriteria } from '../../../../../src/app/core/models/tutor-search.models';

describe('TutorFilterBarComponent', () => {
  let component: TutorFilterBarComponent;
  let fixture: ComponentFixture<TutorFilterBarComponent>;
  let emitted: TutorFilterCriteria[];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TutorFilterBarComponent] }).compileComponents();

    fixture = TestBed.createComponent(TutorFilterBarComponent);
    component = fixture.componentInstance;
    emitted = [];
    component.criteriaChange.subscribe((criteria) => emitted.push(criteria));
    fixture.detectChanges();
  });

  it('Given_ChangedFilters_When_Applying_Then_EmitsCurrentCriteria', () => {
    component.form.setValue({ subject: 'SQL', maxPrice: 90, date: '2026-11-10', sortBy: 'priceDesc' });

    component.apply();

    expect(emitted).toEqual([{ subject: 'SQL', maxPrice: 90, date: '2026-11-10', sortBy: 'priceDesc' }]);
  });

  it('Given_InvalidMaxPrice_When_Applying_Then_DoesNotEmit', () => {
    component.form.controls.maxPrice.setValue(0);

    component.apply();

    expect(emitted).toEqual([]);
    expect(component.form.controls.maxPrice.touched).toBe(true);
  });

  it('Given_ChangedFilters_When_Resetting_Then_EmitsDefaultCriteria', () => {
    component.form.controls.subject.setValue('Java');
    component.form.controls.maxPrice.setValue(50);

    component.reset();

    expect(emitted).toEqual([DEFAULT_FILTER_CRITERIA]);
    expect(component.form.getRawValue()).toEqual(DEFAULT_FILTER_CRITERIA);
  });

  it('Given_FilledForm_When_SubmittingFromDom_Then_EmitsCriteria', () => {
    component.form.controls.subject.setValue('C++');
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    form.dispatchEvent(new Event('submit'));

    expect(emitted.length).toBe(1);
    expect(emitted[0].subject).toBe('C++');
  });

  const query = <T extends HTMLElement>(selector: string): T => fixture.nativeElement.querySelector(selector) as T;

  it('Given_NewFilterBar_When_Rendering_Then_ShowsDefaultValuesAndSortOptions', () => {
    expect(query<HTMLInputElement>('#filter-subject').value).toBe('');
    expect(query<HTMLInputElement>('#filter-max-price').value).toBe('');
    expect(query<HTMLInputElement>('#filter-date').value).toBe('');
    expect(query<HTMLSelectElement>('#filter-sort').value).toBe('priceAsc');
    expect(
      Array.from(query<HTMLSelectElement>('#filter-sort').options).map((option) => option.value)
    ).toEqual(['priceAsc', 'priceDesc']);
  });

  it('Given_FormWithLabels_When_Rendering_Then_EveryFieldHasAssociatedLabel', () => {
    const labels = Array.from(fixture.nativeElement.querySelectorAll('label') as NodeListOf<HTMLLabelElement>);

    expect(labels.map((label) => label.htmlFor)).toEqual([
      'filter-subject',
      'filter-max-price',
      'filter-date',
      'filter-sort'
    ]);
    expect(query('form').getAttribute('aria-label')).toBe('Tutor filters');
  });

  it('Given_ValuesTypedInDom_When_ClickingApplyFilters_Then_EmitsTypedCriteria', () => {
    const subject = query<HTMLInputElement>('#filter-subject');
    subject.value = 'SQL';
    subject.dispatchEvent(new Event('input'));
    const maxPrice = query<HTMLInputElement>('#filter-max-price');
    maxPrice.value = '90';
    maxPrice.dispatchEvent(new Event('input'));
    const date = query<HTMLInputElement>('#filter-date');
    date.value = '2026-11-10';
    date.dispatchEvent(new Event('input'));
    const sort = query<HTMLSelectElement>('#filter-sort');
    sort.value = 'priceDesc';
    sort.dispatchEvent(new Event('change'));

    query<HTMLFormElement>('form').dispatchEvent(new Event('submit'));

    expect(emitted).toEqual([{ subject: 'SQL', maxPrice: 90, date: '2026-11-10', sortBy: 'priceDesc' }]);
  });

  it('Given_Form_When_Rendering_Then_ApplyIsSubmitButtonAndResetIsPlainButton', () => {
    expect(query<HTMLButtonElement>('.filter-actions .btn-primary').type).toBe('submit');
    expect(query<HTMLButtonElement>('.filter-actions .btn-primary').textContent).toContain('Apply filters');
    expect(query<HTMLButtonElement>('.filter-actions .btn-secondary').type).toBe('button');
    expect(query<HTMLButtonElement>('.filter-actions .btn-secondary').textContent).toContain('Reset');
  });

  it('Given_ChangedFilters_When_ClickingResetButton_Then_RestoresInputsAndEmitsDefaults', () => {
    component.form.setValue({ subject: 'Java', maxPrice: 50, date: '2026-11-10', sortBy: 'priceDesc' });
    fixture.detectChanges();

    query<HTMLButtonElement>('.filter-actions .btn-secondary').click();
    fixture.detectChanges();

    expect(emitted).toEqual([DEFAULT_FILTER_CRITERIA]);
    expect(query<HTMLInputElement>('#filter-subject').value).toBe('');
    expect(query<HTMLInputElement>('#filter-max-price').value).toBe('');
    expect(query<HTMLSelectElement>('#filter-sort').value).toBe('priceAsc');
  });

  it('Given_InvalidMaxPrice_When_Applying_Then_ShowsInlineErrorAndMarksFieldInvalid', () => {
    component.form.controls.maxPrice.setValue(0);

    component.apply();
    fixture.detectChanges();

    expect(query('.error-feedback').textContent).toContain('Max price must be at least 1 RON.');
    expect(query('#filter-max-price').classList.contains('is-invalid')).toBe(true);
  });

  it('Given_ValidMaxPrice_When_Rendering_Then_ShowsNoInlineError', () => {
    component.form.controls.maxPrice.setValue(1);
    component.form.controls.maxPrice.markAsTouched();

    fixture.detectChanges();

    expect(query('.error-feedback')).toBeNull();
    expect(query('#filter-max-price').classList.contains('is-invalid')).toBe(false);
  });

  it('Given_InvalidMaxPriceShown_When_CorrectingValue_Then_ApplyEmitsAndErrorDisappears', () => {
    component.form.controls.maxPrice.setValue(0);
    component.apply();
    component.form.controls.maxPrice.setValue(25);

    component.apply();
    fixture.detectChanges();

    expect(emitted).toEqual([{ ...DEFAULT_FILTER_CRITERIA, maxPrice: 25 }]);
    expect(query('.error-feedback')).toBeNull();
  });
});
