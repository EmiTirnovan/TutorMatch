import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TutorCardComponent } from '../../../../../src/app/features/tutors/components/tutor-card/tutor-card.component';
import { TutorSearchItem } from '../../../../../src/app/core/models/tutor-search.models';

describe('TutorCardComponent', () => {
  let fixture: ComponentFixture<TutorCardComponent>;

  const createCard = (tutor: Omit<TutorSearchItem, 'subjects'> & Partial<Pick<TutorSearchItem, 'subjects'>>): HTMLElement => {
    fixture = TestBed.createComponent(TutorCardComponent);
    fixture.componentRef.setInput('tutor', { subjects: [], ...tutor });
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

  it.each([
    ['Ana Maria Pop', 'AM'],
    ['ana pop', 'AP'],
    ['Madonna', 'M']
  ])('Given_TutorNamed_%s_When_Rendering_Then_ShowsInitials_%s', (name, expected) => {
    const card = createCard({ id: 'tutor-4', name, hourlyRate: 60, averageRating: 0, ratingCount: 0 });

    expect(card.querySelector('.avatar-circle')?.textContent?.trim()).toBe(expected);
  });

  it('Given_RatedTutor_When_Rendering_Then_ExposesAccessibleRatingSummary', () => {
    const card = createCard({ id: 'tutor-5', name: 'Ana Pop', hourlyRate: 60, averageRating: 4, ratingCount: 3 });

    expect(card.querySelector('.rating')?.getAttribute('aria-label')).toBe('4.0 out of 5, 3 ratings');
    expect(card.querySelector('.rating strong')?.textContent).toBe('4.0');
    expect(card.querySelectorAll('.rating svg.star').length).toBe(5);
    expect(card.querySelectorAll('.rating .star-filled').length).toBe(4);
  });

  it('Given_TutorWithoutRatings_When_Rendering_Then_ShowsNoStars', () => {
    const card = createCard({ id: 'tutor-6', name: 'Ana Pop', hourlyRate: 60, averageRating: 0, ratingCount: 0 });

    expect(card.querySelectorAll('svg.star').length).toBe(0);
    expect(fixture.componentInstance.hasRatings()).toBe(false);
  });

  it('Given_TutorWithEmptySubjects_When_Rendering_Then_HidesSubjectTags', () => {
    const card = createCard({ id: 'tutor-7', name: 'Ana Pop', hourlyRate: 60, averageRating: 3, ratingCount: 1, subjects: [] });

    expect(card.querySelector('.subjects')).toBeNull();
  });

  it('Given_TutorWithSubjects_When_Rendering_Then_ShowsOneTagPerSubjectInGivenOrder', () => {
    const card = createCard({
      id: 'tutor-9',
      name: 'Ana Pop',
      hourlyRate: 60,
      averageRating: 3,
      ratingCount: 1,
      subjects: ['Algoritmi', 'Fizica', 'SQL']
    });

    const tags = Array.from(card.querySelectorAll('.subjects .badge')).map((tag) => tag.textContent?.trim());
    expect(tags).toEqual(['Algoritmi', 'Fizica', 'SQL']);
  });

  it('Given_Tutor_When_Rendering_Then_ShowsHourlyPriceAndProfileLinkLabel', () => {
    const card = createCard({ id: 'tutor-8', name: 'Ana Pop', hourlyRate: 120, averageRating: 5, ratingCount: 9 });

    expect(card.querySelector('.price')?.textContent).toContain('120 RON');
    expect(card.querySelector('.price')?.textContent).toContain('/ hour');
    expect(card.querySelector('a.btn')?.textContent).toContain('View profile');
    expect(card.querySelector('a.btn')?.getAttribute('href')).toBe('/tutors/tutor-8');
  });
});
