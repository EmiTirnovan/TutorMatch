import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from './auth.service';

export type UserRole = 'student' | 'tutor';

@Injectable({
  providedIn: 'root'
})
export class UserContextService {
  private readonly authService = inject(AuthService);

  // Overrides for unit tests or manual switching
  private readonly overrideRole = signal<UserRole | null>(null);
  private readonly overrideStudentId = signal<string | null>(null);
  private readonly overrideTutorId = signal<string | null>(null);

  readonly currentRole = computed<UserRole>(() => {
    if (this.overrideRole()) return this.overrideRole()!;
    const userRole = this.authService.currentUser()?.role?.toLowerCase();
    return userRole === 'tutor' ? 'tutor' : 'student';
  });

  readonly studentId = computed<string>(() => {
    return this.overrideStudentId() ?? this.authService.currentUser()?.id ?? '';
  });

  readonly tutorId = computed<string>(() => {
    return this.overrideTutorId() ?? this.authService.currentUser()?.id ?? '';
  });

  readonly studentName = signal<string>('Alex Radu');
  readonly tutorName = signal<string>('Emi Tirnovan');

  readonly currentUserId = computed(() =>
    this.currentRole() === 'student' ? this.studentId() : this.tutorId()
  );

  readonly currentUserName = computed(() => {
    const authName = this.authService.currentUser()?.fullName;
    if (authName) return authName;
    return this.currentRole() === 'student' ? this.studentName() : this.tutorName();
  });

  readonly roleDisplayName = computed(() =>
    this.currentRole() === 'tutor' ? 'Tutor View' : 'Student View'
  );

  setRole(role: UserRole): void {
    this.overrideRole.set(role);
  }

  setStudentId(id: string): void {
    this.overrideStudentId.set(id);
  }

  setTutorId(id: string): void {
    this.overrideTutorId.set(id);
  }

  clearOverrides(): void {
    this.overrideRole.set(null);
    this.overrideStudentId.set(null);
    this.overrideTutorId.set(null);
  }
}
