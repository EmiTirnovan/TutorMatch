import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StarRatingComponent } from '../../../../../src/app/features/tutors/components/star-rating/star-rating.component';

describe('StarRatingComponent', () => {
  let fixture: ComponentFixture<StarRatingComponent>;

  const render = (value: number, size?: number): void => {
    fixture = TestBed.createComponent(StarRatingComponent);
    fixture.componentRef.setInput('value', value);
    if (size !== undefined) {
      fixture.componentRef.setInput('size', size);
    }
    fixture.detectChanges();
  };

  const filled = (): number => fixture.nativeElement.querySelectorAll('.star-filled').length;
  const total = (): number => fixture.nativeElement.querySelectorAll('svg.star').length;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [StarRatingComponent] }).compileComponents();
  });

  it.each([0, 1, 2, 3, 4, 5])('Given_ValueOf_%s_When_Rendering_Then_FillsThatManyOfFiveStars', (value) => {
    render(value);

    expect(total()).toBe(5);
    expect(filled()).toBe(value);
  });

  it.each([
    [4.4, 4],
    [4.5, 5],
    [4.9, 5],
    [0.4, 0]
  ])('Given_FractionalValue_%s_When_Rendering_Then_RoundsToNearestStar_%s', (value, expected) => {
    render(value);

    expect(filled()).toBe(expected);
  });

  it.each([
    [-3, 0],
    [9, 5]
  ])('Given_OutOfRangeValue_%s_When_Rendering_Then_ClampsToZeroToFive_%s', (value, expected) => {
    render(value);

    expect(filled()).toBe(expected);
  });

  it('Given_NoSize_When_Rendering_Then_UsesSixteenPixelStars', () => {
    render(3);

    const star = fixture.nativeElement.querySelector('svg.star') as SVGElement;
    expect(star.getAttribute('width')).toBe('16');
    expect(star.getAttribute('height')).toBe('16');
  });

  it('Given_CustomSize_When_Rendering_Then_AppliesItToEveryStar', () => {
    render(3, 24);

    const sizes = Array.from(fixture.nativeElement.querySelectorAll('svg.star') as NodeListOf<SVGElement>).map(
      (star) => star.getAttribute('width')
    );
    expect(sizes).toEqual(['24', '24', '24', '24', '24']);
  });

  it('Given_Stars_When_Rendering_Then_AreHiddenFromAssistiveTechnology', () => {
    render(5);

    expect(fixture.nativeElement.querySelector('.stars').getAttribute('aria-hidden')).toBe('true');
  });
});