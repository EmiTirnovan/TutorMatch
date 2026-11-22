import { Component, inject, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DEFAULT_FILTER_CRITERIA, TutorFilterCriteria, TutorSortOption } from '../../../../core/models/tutor-search.models';
import { SubjectAutocompleteComponent } from '../subject-autocomplete/subject-autocomplete.component';

@Component({
  selector: 'app-tutor-filter-bar',
  standalone: true,
  imports: [ReactiveFormsModule, SubjectAutocompleteComponent],
  templateUrl: './tutor-filter-bar.component.html',
  styleUrl: './tutor-filter-bar.component.css'
})
export class TutorFilterBarComponent {
  private readonly fb = inject(FormBuilder);

  readonly criteriaChange = output<TutorFilterCriteria>();

  readonly form = this.fb.group({
    subject: this.fb.nonNullable.control(DEFAULT_FILTER_CRITERIA.subject),
    maxPrice: this.fb.control<number | null>(DEFAULT_FILTER_CRITERIA.maxPrice, [Validators.min(1)]),
    minRating: this.fb.control<number | null>(DEFAULT_FILTER_CRITERIA.minRating),
    date: this.fb.nonNullable.control(DEFAULT_FILTER_CRITERIA.date),
    sortBy: this.fb.nonNullable.control<TutorSortOption>(DEFAULT_FILTER_CRITERIA.sortBy)
  });

  readonly ratingOptions = [1, 2, 3, 4, 5];
  readonly selectedRating = toSignal(this.form.controls.minRating.valueChanges, {
    initialValue: this.form.controls.minRating.value
  });

  /** Picking the already selected star clears the rating filter. */
  selectRating(rating: number): void {
    const current = this.form.controls.minRating.value;
    this.form.controls.minRating.setValue(current === rating ? null : rating);
  }

  apply(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.criteriaChange.emit(this.form.getRawValue());
  }

  reset(): void {
    this.form.reset(DEFAULT_FILTER_CRITERIA);
    this.criteriaChange.emit(this.form.getRawValue());
  }
}
