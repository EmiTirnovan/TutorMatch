import { Component, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TutorService } from '../../../../core/services/tutor.service';
import { CreateTutorRequest, CreateTutorResponse } from '../../../../core/models/tutor.models';
import { AvailabilityEditorComponent } from '../availability-editor/availability-editor.component';

@Component({
  selector: 'app-tutor-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink, AvailabilityEditorComponent],
  templateUrl: './tutor-register.component.html',
  styleUrl: './tutor-register.component.css'
})
export class TutorRegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly tutorService = inject(TutorService);
  private readonly router = inject(Router);

  @ViewChild(AvailabilityEditorComponent) availabilityEditor?: AvailabilityEditorComponent;

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successTutorId = signal<string | null>(null);

  // Subject tags state
  readonly subjects = signal<string[]>(['Algoritmi', 'Structuri de date', 'C++']);
  newSubjectInput: string = '';

  readonly registerForm: FormGroup = this.fb.group({
    name: ['Alex Popescu', [Validators.required, Validators.minLength(2)]],
    email: ['alex.popescu@student.upt.ro', [Validators.required, Validators.email]],
    bio: [
      'Student în anul 3 la Calculatoare, pasionat de algoritmi, grafuri și pregătire pentru laboratoare și examene.',
      [Validators.required, Validators.minLength(10)]
    ],
    hourlyRate: [85, [Validators.required, Validators.min(1), Validators.max(1000)]]
  });

  addSubject(): void {
    const trimmed = this.newSubjectInput.trim();
    if (!trimmed) {
      return;
    }

    if (!this.subjects().some(s => s.toLowerCase() === trimmed.toLowerCase())) {
      this.subjects.update(curr => [...curr, trimmed]);
    }
    this.newSubjectInput = '';
  }

  removeSubject(index: number): void {
    this.subjects.update(curr => curr.filter((_, i) => i !== index));
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    if (this.subjects().length === 0) {
      this.errorMessage.set('Please add at least one subject or topic you teach.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successTutorId.set(null);

    const formVal = this.registerForm.value;
    const initialSlots = this.availabilityEditor ? this.availabilityEditor.slots() : [];

    const request: CreateTutorRequest = {
      name: formVal.name,
      email: formVal.email,
      bio: formVal.bio,
      hourlyRate: Number(formVal.hourlyRate),
      subjects: this.subjects(),
      availability: initialSlots
    };

    this.tutorService.createTutor(request).subscribe({
      next: (response: CreateTutorResponse) => {
        this.isSubmitting.set(false);
        this.successTutorId.set(response.id);
        try {
          localStorage.setItem('tutor_match_my_tutor_id', response.id);
        } catch {
          // ignore in environments without localStorage
        }
      },
      error: (err: { error?: { detail?: string; title?: string } }) => {
        this.isSubmitting.set(false);
        const detail = err?.error?.detail || err?.error?.title || 'Registration failed. Please check your data.';
        this.errorMessage.set(detail);
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
}
