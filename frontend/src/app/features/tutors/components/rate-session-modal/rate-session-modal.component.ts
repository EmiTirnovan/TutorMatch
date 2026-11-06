import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RateSessionRequest } from '../../../../core/models/tutor-search.models';
import { TutorService } from '../../../../core/services/tutor.service';

export const MIN_RATING = 1;
export const MAX_RATING = 5;
const COMPLETED_STATUS = 'Completed';

@Component({
  selector: 'app-rate-session-modal',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './rate-session-modal.component.html',
  styleUrl: './rate-session-modal.component.css'
})
export class RateSessionModalComponent implements AfterViewInit {
  private readonly fb = inject(FormBuilder);
  private readonly tutorService = inject(TutorService);
  private readonly dialog = viewChild<ElementRef<HTMLElement>>('dialog');

  readonly tutorId = input.required<string>();
  readonly sessionId = input.required<string>();
  readonly studentId = input.required<string>();
  readonly sessionStatus = input.required<string>();

  readonly closed = output<void>();
  readonly rated = output<RateSessionRequest>();

  readonly starValues = Array.from({ length: MAX_RATING }, (_, index) => index + MIN_RATING);
  readonly canRate = computed(() => this.sessionStatus() === COMPLETED_STATUS);

  readonly isSubmitting = signal(false);
  readonly isRated = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    score: this.fb.nonNullable.control(0, [Validators.min(MIN_RATING), Validators.max(MAX_RATING)]),
    notes: this.fb.nonNullable.control('')
  });

  ngAfterViewInit(): void {
    this.dialog()?.nativeElement.focus();
  }

  selectScore(score: number): void {
    if (this.isRated()) {
      return;
    }
    this.form.controls.score.setValue(score);
    this.form.controls.score.markAsTouched();
  }

  submit(): void {
    if (!this.canRate() || this.isSubmitting() || this.isRated()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { score, notes } = this.form.getRawValue();
    const trimmedNotes = notes.trim();
    const request: RateSessionRequest = trimmedNotes ? { score, notes: trimmedNotes } : { score };

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.tutorService.rateSession(this.tutorId(), this.sessionId(), this.studentId(), request).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.isRated.set(true);
        this.rated.emit(request);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        if (error.status === 409) {
          this.isRated.set(true);
        }
        this.errorMessage.set(error.error?.detail ?? 'The rating could not be saved. Please try again.');
      }
    });
  }

  close(): void {
    this.closed.emit();
  }
}
