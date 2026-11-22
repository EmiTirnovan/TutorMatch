import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { EditProfileComponent } from '../../../../../src/app/features/tutors/components/edit-profile/edit-profile.component';
import { TutorService } from '../../../../../src/app/core/services/tutor.service';
import { StudentService } from '../../../../../src/app/core/services/student.service';
import { UserContextService } from '../../../../../src/app/core/services/user-context.service';
import { TutorDetailsResponse } from '../../../../../src/app/core/models/tutor.models';

describe('EditProfileComponent', () => {
  let component: EditProfileComponent;
  let fixture: ComponentFixture<EditProfileComponent>;
  let tutorService: TutorService;
  let studentService: StudentService;
  let userContext: UserContextService;

  const mockTutorData: TutorDetailsResponse = {
    id: 'tutor-123',
    name: 'Andrei Popescu',
    email: 'andrei@tutormatch.ro',
    bio: 'Experienced tutor with extensive background in algorithms and graph theory.',
    hourlyRate: 110,
    subjects: ['Algoritmi', 'Structuri de date'],
    availability: []
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditProfileComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        TutorService,
        StudentService,
        UserContextService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EditProfileComponent);
    component = fixture.componentInstance;
    tutorService = TestBed.inject(TutorService);
    studentService = TestBed.inject(StudentService);
    userContext = TestBed.inject(UserContextService);
  });

  describe('Tutor Role Mode', () => {
    beforeEach(() => {
      userContext.setRole('tutor');
      userContext.setTutorId('tutor-123');
      vi.spyOn(tutorService, 'getTutorById').mockReturnValue(of(mockTutorData));
      fixture.detectChanges();
    });

    it('Given_TutorRole_When_Loaded_Then_PopulatesFormWithTutorDetailsAndSubjects', () => {
      expect(component.isTutor()).toBe(true);
      expect(component.profileForm.get('name')?.value).toBe('Andrei Popescu');
      expect(component.profileForm.get('bio')?.value).toContain('Experienced tutor');
      expect(component.profileForm.get('hourlyRate')?.value).toBe(110);
      expect(component.subjects()).toEqual(['Algoritmi', 'Structuri de date']);
      expect(component.isLoading()).toBe(false);
    });

    it('Given_InvalidForm_When_Submitting_Then_MarksFieldsAsTouchedAndDoesNotCallService', () => {
      component.profileForm.patchValue({
        name: '',
        hourlyRate: -10,
        bio: 'Too short'
      });

      const updateSpy = vi.spyOn(tutorService, 'updateTutor');
      component.onSubmit();

      expect(updateSpy).not.toHaveBeenCalled();
      expect(component.isFieldInvalid('name')).toBe(true);
      expect(component.isFieldInvalid('hourlyRate')).toBe(true);
      expect(component.isFieldInvalid('bio')).toBe(true);
    });

    it('Given_NoSubjects_When_Submitting_Then_SetsErrorMessageAndDoesNotCallService', () => {
      component.subjects.set([]);
      const updateSpy = vi.spyOn(tutorService, 'updateTutor');

      component.onSubmit();

      expect(updateSpy).not.toHaveBeenCalled();
      expect(component.errorMessage()).toContain('at least one subject');
    });

    it('Given_SubjectSelectedFromAutocomplete_When_Adding_Then_AppendsDistinctChip', () => {
      component.onSubjectSelected('Machine Learning');

      expect(component.subjects()).toContain('Machine Learning');

      // Duplicate case-insensitive
      component.onSubjectSelected('machine learning');
      expect(component.subjects().filter(s => s.toLowerCase() === 'machine learning').length).toBe(1);
    });

    it('Given_ExistingSubjectChip_When_Removing_Then_RemovesSubject', () => {
      component.removeSubject(0);
      expect(component.subjects()).toEqual(['Structuri de date']);
    });

    it('Given_ValidData_When_Submitting_Then_CallsUpdateTutorAndUpdatesUserContext', () => {
      const updateSpy = vi.spyOn(tutorService, 'updateTutor').mockReturnValue(of(undefined));

      component.profileForm.patchValue({
        name: 'Andrei Popescu PhD',
        bio: 'Updated bio with more than 10 characters length.',
        hourlyRate: 150
      });

      component.onSubmit();

      expect(updateSpy).toHaveBeenCalledWith('tutor-123', {
        name: 'Andrei Popescu PhD',
        bio: 'Updated bio with more than 10 characters length.',
        hourlyRate: 150,
        subjects: ['Algoritmi', 'Structuri de date']
      });

      expect(userContext.tutorName()).toBe('Andrei Popescu PhD');
      expect(component.successMessage()).toContain('updated successfully');
      expect(component.isSaving()).toBe(false);
    });

    it('Given_ApiFailure_When_Submitting_Then_DisplaysErrorMessage', () => {
      vi.spyOn(tutorService, 'updateTutor').mockReturnValue(
        throwError(() => ({ error: { detail: 'Database error occurred.' } }))
      );

      component.onSubmit();

      expect(component.errorMessage()).toBe('Database error occurred.');
      expect(component.isSaving()).toBe(false);
    });
  });

  describe('Student Role Mode', () => {
    beforeEach(() => {
      userContext.setRole('student');
      userContext.setStudentId('student-456');
      vi.spyOn(studentService, 'getStudentById').mockReturnValue(
        of({ id: 'student-456', name: 'Maria Ionescu', email: 'maria@student.upt.ro' })
      );
      fixture.detectChanges();
    });

    it('Given_StudentRole_When_Loaded_Then_InitializesStudentFormFieldsOnly', () => {
      expect(component.isTutor()).toBe(false);
      expect(component.profileForm.get('name')?.value).toBe('Maria Ionescu');
      expect(component.profileForm.get('email')?.value).toBe('maria@student.upt.ro');
      expect(component.profileForm.get('bio')).toBeNull();
      expect(component.profileForm.get('hourlyRate')).toBeNull();
    });

    it('Given_ValidData_When_SubmittingStudent_Then_CallsStudentServiceAndUpdatesContext', () => {
      const updateSpy = vi.spyOn(studentService, 'updateStudentProfile').mockReturnValue(of(undefined));

      component.profileForm.patchValue({
        name: 'Maria Ionescu MSc'
      });

      component.onSubmit();

      expect(updateSpy).toHaveBeenCalledWith('student-456', {
        name: 'Maria Ionescu MSc'
      });
      expect(userContext.studentName()).toBe('Maria Ionescu MSc');
      expect(component.successMessage()).toContain('Student profile updated successfully');
    });
  });
});
