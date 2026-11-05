import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { TutorProfileComponent } from './tutor-profile.component';
import { TutorService } from '../../../../core/services/tutor.service';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';

describe('TutorProfileComponent', () => {
  let component: TutorProfileComponent;
  let fixture: ComponentFixture<TutorProfileComponent>;
  let tutorService: TutorService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorProfileComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'id' ? 'tutor-xyz' : null)
              }
            }
          }
        },
        TutorService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TutorProfileComponent);
    component = fixture.componentInstance;
    tutorService = TestBed.inject(TutorService);
  });

  it('should create the profile component', () => {
    expect(component).toBeTruthy();
  });

  it('Given_TutorIdInRoute_When_Initialized_Then_FetchesTutorDetails', () => {
    const mockTutor: TutorDetailsResponse = {
      id: 'tutor-xyz',
      name: 'Maria Dragomir',
      email: 'maria@test.ro',
      bio: 'Math and Discrete Mathematics tutor.',
      hourlyRate: 75,
      subjects: ['Discrete Math'],
      rating: 5.0,
      ratingCount: 3,
      availability: []
    };

    const getSpy = vi.spyOn(tutorService, 'getTutorById').mockReturnValue(of(mockTutor));

    fixture.detectChanges();

    expect(getSpy).toHaveBeenCalledWith('tutor-xyz');
    expect(component.tutor()).toEqual(mockTutor);
    expect(component.isLoading()).toBe(false);
  });

  it('Given_FullName_When_GettingInitials_Then_ReturnsTwoLetters', () => {
    expect(component.getInitials('Maria Dragomir')).toBe('MD');
    expect(component.getInitials('Radu')).toBe('R');
  });
});
