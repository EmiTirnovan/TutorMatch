import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TutorFilterBarComponent } from './tutor-filter-bar.component';
import { DEFAULT_FILTER_CRITERIA, TutorFilterCriteria } from '../../../../core/models/tutor-search.models';

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
});
