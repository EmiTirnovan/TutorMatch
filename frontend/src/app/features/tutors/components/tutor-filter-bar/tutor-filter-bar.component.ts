import { Component, inject, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DEFAULT_FILTER_CRITERIA, TutorFilterCriteria, TutorSortOption } from '../../../../core/models/tutor-search.models';

@Component({
  selector: 'app-tutor-filter-bar',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './tutor-filter-bar.component.html',
  styleUrl: './tutor-filter-bar.component.css'
})
export class TutorFilterBarComponent {
  private readonly fb = inject(FormBuilder);

  readonly criteriaChange = output<TutorFilterCriteria>();

  readonly form = this.fb.group({
    subject: this.fb.nonNullable.control(DEFAULT_FILTER_CRITERIA.subject),
    maxPrice: this.fb.control<number | null>(DEFAULT_FILTER_CRITERIA.maxPrice, [Validators.min(1)]),
    date: this.fb.nonNullable.control(DEFAULT_FILTER_CRITERIA.date),
    sortBy: this.fb.nonNullable.control<TutorSortOption>(DEFAULT_FILTER_CRITERIA.sortBy)
  });

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
