import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UserContextService } from '../../../../core/services/user-context.service';
import { TutorService } from '../../../../core/services/tutor.service';
import { StudentService } from '../../../../core/services/student.service';
import { SubjectAutocompleteComponent } from '../subject-autocomplete/subject-autocomplete.component';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, SubjectAutocompleteComponent],
  templateUrl: './edit-profile.component.html',
  styleUrl: './edit-profile.component.css'
})
export class EditProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly userContext = inject(UserContextService);
  private readonly tutorService = inject(TutorService);
  private readonly studentService = inject(StudentService);
  private readonly router = inject(Router);

  readonly userRole = this.userContext.currentRole;
  readonly isTutor = computed(() => this.userRole() === 'tutor');

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Subjects for tutor profile
  readonly subjects = signal<string[]>([]);

  profileForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadProfile();
  }

  private initForm(): void {
    if (this.isTutor()) {
      this.profileForm = this.fb.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        email: [{ value: '', disabled: true }],
        bio: ['', [Validators.required, Validators.minLength(10)]],
        hourlyRate: [50, [Validators.required, Validators.min(1), Validators.max(1000)]]
      });
    } else {
      this.profileForm = this.fb.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        email: [{ value: '', disabled: true }]
      });
    }
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    if (this.isTutor()) {
      const tutorId = this.userContext.tutorId();
      this.tutorService.getTutorById(tutorId).subscribe({
        next: tutor => {
          this.profileForm.patchValue({
            name: tutor.name,
            email: tutor.email,
            bio: tutor.bio,
            hourlyRate: tutor.hourlyRate
          });
          this.subjects.set(tutor.subjects ? [...tutor.subjects] : []);
          this.isLoading.set(false);
        },
        error: () => {
          // If demo tutor or not yet persisted
          this.profileForm.patchValue({
            name: this.userContext.tutorName(),
            email: 'tutor@example.com',
            bio: 'Student at university helping with technical courses and exam preparation.',
            hourlyRate: 80
          });
          this.subjects.set(['Algoritmi', 'POO']);
          this.isLoading.set(false);
        }
      });
    } else {
      const studentId = this.userContext.studentId();
      this.studentService.getStudentById(studentId).subscribe({
        next: student => {
          this.profileForm.patchValue({
            name: student.name,
            email: student.email
          });
          this.isLoading.set(false);
        },
        error: () => {
          this.profileForm.patchValue({
            name: this.userContext.studentName(),
            email: 'student@example.com'
          });
          this.isLoading.set(false);
        }
      });
    }
  }

  onSubjectSelected(newSubject: string): void {
    const trimmed = newSubject.trim();
    if (!trimmed) return;

    if (!this.subjects().some(s => s.toLowerCase() === trimmed.toLowerCase())) {
      this.subjects.update(curr => [...curr, trimmed]);
    }
  }

  removeSubject(index: number): void {
    this.subjects.update(curr => curr.filter((_, i) => i !== index));
  }

  isFieldInvalid(field: string): boolean {
    const control = this.profileForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  onSubmit(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    if (this.isTutor() && this.subjects().length === 0) {
      this.errorMessage.set('Please select or add at least one subject you teach.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formValues = this.profileForm.getRawValue();

    if (this.isTutor()) {
      const tutorId = this.userContext.tutorId();
      this.tutorService.updateTutor(tutorId, {
        name: formValues.name.trim(),
        bio: formValues.bio.trim(),
        hourlyRate: Number(formValues.hourlyRate),
        subjects: this.subjects()
      }).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.userContext.tutorName.set(formValues.name.trim());
          this.successMessage.set('Tutor profile and subjects updated successfully!');
        },
        error: err => {
          this.isSaving.set(false);
          const detail = err?.error?.detail || err?.error?.title || 'Failed to update tutor profile.';
          this.errorMessage.set(detail);
        }
      });
    } else {
      const studentId = this.userContext.studentId();
      this.studentService.updateStudentProfile(studentId, {
        name: formValues.name.trim()
      }).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.userContext.studentName.set(formValues.name.trim());
          this.successMessage.set('Student profile updated successfully!');
        },
        error: err => {
          this.isSaving.set(false);
          const detail = err?.error?.detail || err?.error?.title || 'Failed to update student profile.';
          this.errorMessage.set(detail);
        }
      });
    }
  }
}
