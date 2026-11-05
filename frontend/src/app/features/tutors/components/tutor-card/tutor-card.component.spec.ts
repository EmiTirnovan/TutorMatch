import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TutorCardComponent } from './tutor-card.component';
import { TutorSearchItem } from '../../../../core/models/tutor-search.models';

describe('TutorCardComponent', () => {
  let fixture: ComponentFixture<TutorCardComponent>;

  const createCard = (tutor: TutorSearchItem): HTMLElement => {
    fixture = TestBed.createComponent(TutorCardComponent);
    fixture.componentRef.setInput('tutor', tutor);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorCardComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('Given_RatedTutor_When_Rendering_Then_ShowsNameRatingPriceAndSubjects', () => {
    const card = createCard({
      id: 'tutor-1',
      name: 'Maria Dragomir',
      hourlyRate: 75,
      averageRating: 4.66,
      ratingCount: 12,
      subjects: ['Algoritmi', 'SQL']
    });

    expect(card.querySelector('.avatar-circle')?.textContent).toContain('MD');
    expect(card.querySelector('.tutor-name')?.textContent).toContain('Maria Dragomir');
    expect(card.querySelector('.rating')?.textContent).toContain('4.7');
    expect(card.querySelector('.rating')?.textContent).toContain('(12)');
    expect(card.querySelectorAll('.star-filled').length).toBe(5);
    expect(card.querySelectorAll('.subjects .badge').length).toBe(2);
    expect(card.querySelector('.price')?.textContent).toContain('75 RON');
  });

  it('Given_Tutor_When_Rendering_Then_LinksToProfile', () => {
    const card = createCard({ id: 'tutor-1', name: 'Radu Ionescu', hourlyRate: 80, averageRating: 0, ratingCount: 0 });

    expect(card.querySelector('a')?.getAttribute('href')).toBe('/tutors/tutor-1');
  });

  it('Given_TutorWithoutRatings_When_Rendering_Then_ShowsNoRatingsYet', () => {
    const card = createCard({ id: 'tutor-2', name: 'Radu Ionescu', hourlyRate: 80, averageRating: 0, ratingCount: 0 });

    expect(card.textContent).toContain('No ratings yet');
    expect(card.querySelector('.rating')).toBeNull();
    expect(card.querySelector('.subjects')).toBeNull();
  });

  it('Given_AverageRating_When_ComputingStars_Then_RoundsToFilledStars', () => {
    createCard({ id: 'tutor-3', name: 'Ana Pop', hourlyRate: 60, averageRating: 3.4, ratingCount: 5 });

    expect(fixture.componentInstance.stars()).toEqual([true, true, true, false, false]);
  });
});
