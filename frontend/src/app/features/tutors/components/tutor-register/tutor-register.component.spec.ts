import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { TutorRegisterComponent } from './tutor-register.component';
import { TutorService } from '../../../../core/services/tutor.service';
import { CreateTutorResponse } from '../../../../core/models/tutor.models';

describe('TutorRegisterComponent', () => {
  let component: TutorRegisterComponent;
  let fixture: ComponentFixture<TutorRegisterComponent>;
  let tutorService: TutorService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorRegisterComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        TutorService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TutorRegisterComponent);
    component = fixture.componentInstance;
    tutorService = TestBed.inject(TutorService);
    fixture.detectChanges();
  });

  it('should create the register component', () => {
    expect(component).toBeTruthy();
  });

  it('Given_InvalidForm_When_Submitted_Then_DoesNotCallService', () => {
    component.registerForm.patchValue({
      name: '',
      email: 'not-an-email',
      bio: '',
      hourlyRate: -5
    });

    const createSpy = vi.spyOn(tutorService, 'createTutor');

    component.onSubmit();

    expect(createSpy).not.toHaveBeenCalled();
    expect(component.registerForm.invalid).toBe(true);
  });

  it('Given_SubjectInput_When_AddingSubject_Then_AppendsChip', () => {
    component.subjects.set(['C++']);
    component.newSubjectInput = 'Java';

    component.addSubject();

    expect(component.subjects()).toContain('Java');
    expect(component.subjects().length).toBe(2);
    expect(component.newSubjectInput).toBe('');
  });

  it('Given_ExistingSubject_When_RemovingSubject_Then_DeletesChip', () => {
    component.subjects.set(['C++', 'Python']);

    component.removeSubject(0);

    expect(component.subjects()).toEqual(['Python']);
  });

  it('Given_ValidFormAndSubjects_When_Submitted_Then_CallsCreateTutorAndSetsSuccess', () => {
    component.registerForm.setValue({
      name: 'Elena Marin',
      email: 'elena@student.upt.ro',
      bio: 'Pasiune pentru baze de date relationale si SQL.',
      hourlyRate: 95
    });
    component.subjects.set(['SQL', 'Databases']);

    const mockResponse: CreateTutorResponse = {
      id: 'new-tutor-999',
      name: 'Elena Marin',
      email: 'elena@student.upt.ro',
      bio: 'Pasiune pentru baze de date relationale si SQL.',
      hourlyRate: 95
    };

    const createSpy = vi.spyOn(tutorService, 'createTutor').mockReturnValue(of(mockResponse));

    component.onSubmit();

    expect(createSpy).toHaveBeenCalled();
    expect(component.successTutorId()).toBe('new-tutor-999');
    expect(component.errorMessage()).toBeNull();
  });

  it('Given_ServiceFailure_When_Submitted_Then_SetsErrorMessage', () => {
    component.registerForm.setValue({
      name: 'Elena Marin',
      email: 'elena@student.upt.ro',
      bio: 'Pasiune pentru baze de date relationale si SQL.',
      hourlyRate: 95
    });
    component.subjects.set(['SQL']);

    vi.spyOn(tutorService, 'createTutor').mockReturnValue(
      throwError(() => ({ error: { detail: 'Email is already registered.' } }))
    );

    component.onSubmit();

    expect(component.errorMessage()).toBe('Email is already registered.');
    expect(component.successTutorId()).toBeNull();
  });
});
