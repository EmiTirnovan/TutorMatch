import { Injectable, computed, signal } from '@angular/core';
import {
  DEMO_STUDENT_ID,
  DEMO_TUTOR_ID,
  DEMO_STUDENT_NAME,
  DEMO_TUTOR_NAME
} from '../models/session.models';

export type UserRole = 'student' | 'tutor';

@Injectable({
  providedIn: 'root'
})
export class UserContextService {
  readonly currentRole = signal<UserRole>('student');
  readonly studentId = signal<string>(DEMO_STUDENT_ID);
  readonly tutorId = signal<string>(DEMO_TUTOR_ID);
  readonly studentName = signal<string>(DEMO_STUDENT_NAME);
  readonly tutorName = signal<string>(DEMO_TUTOR_NAME);

  readonly currentUserId = computed(() =>
    this.currentRole() === 'student' ? this.studentId() : this.tutorId()
  );

  readonly currentUserName = computed(() =>
    this.currentRole() === 'student' ? this.studentName() : this.tutorName()
  );

  readonly roleDisplayName = computed(() =>
    this.currentRole() === 'tutor' ? 'Tutor View' : 'Student View'
  );

  toggleRole(): void {
    this.currentRole.update(r => (r === 'tutor' ? 'student' : 'tutor'));
  }

  setRole(role: UserRole): void {
    this.currentRole.set(role);
  }

  setStudentId(id: string): void {
    this.studentId.set(id);
  }

  setTutorId(id: string): void {
    this.tutorId.set(id);
  }
}

