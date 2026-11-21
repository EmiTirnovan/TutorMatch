import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TutorReview } from '../../../../../src/app/core/models/tutor.models';
import { TutorReviewsComponent } from '../../../../../src/app/features/tutors/components/tutor-reviews/tutor-reviews.component';

describe('TutorReviewsComponent', () => {
  let fixture: ComponentFixture<TutorReviewsComponent>;

  const review = (id: string, extra: Partial<TutorReview> = {}): TutorReview => ({
    sessionId: id,
    date: '2026-11-05T10:00:00Z',
    score: 5,
    comment: 'Great session',
    studentName: 'Maria Popescu',
    ...extra
  });

  const render = (inputs: {
    averageRating?: number;
    ratingsCount?: number;
    distribution?: Record<string, number> | null;
    reviews?: TutorReview[] | null;
  }): void => {
    fixture = TestBed.createComponent(TutorReviewsComponent);
    Object.entries(inputs).forEach(([name, value]) => fixture.componentRef.setInput(name, value));
    fixture.detectChanges();
  };

  const query = (id: string): HTMLElement | null => fixture.nativeElement.querySelector(`[data-testid="${id}"]`);
  const queryAll = (id: string): HTMLElement[] => Array.from(fixture.nativeElement.querySelectorAll(`[data-testid="${id}"]`));

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TutorReviewsComponent] }).compileComponents();
  });

  describe('Empty state', () => {
    it('Given_TutorWithoutRatings_When_Rendering_Then_ShowsDedicatedEmptyStateInsteadOfSummaryAndList', () => {
      render({ averageRating: 0, ratingsCount: 0, reviews: [] });

      expect(query('reviews-empty')?.textContent).toContain('No reviews yet');
      expect(query('reviews-average')).toBeNull();
      expect(queryAll('distribution-row').length).toBe(0);
      expect(queryAll('review-item').length).toBe(0);
    });

    it('Given_NoInputsAtAll_When_Rendering_Then_ShowsEmptyState', () => {
      render({});

      expect(query('reviews-empty')).not.toBeNull();
    });

    it('Given_NullReviewsAndDistribution_When_Rendering_Then_DoesNotFail', () => {
      render({ ratingsCount: 0, reviews: null, distribution: null });

      expect(query('reviews-empty')).not.toBeNull();
    });
  });

  describe('Summary', () => {
    it('Given_AverageAndCount_When_Rendering_Then_ShowsAverageWithOneDecimalAndReviewCount', () => {
      render({ averageRating: 4.9, ratingsCount: 14, reviews: [review('a')] });

      expect(query('reviews-average')?.textContent?.trim()).toBe('4.9');
      expect(query('reviews-count')?.textContent?.trim()).toBe('14 reviews');
    });

    it('Given_LongFraction_When_Rendering_Then_RoundsAverageToOneDecimal', () => {
      render({ averageRating: 13 / 3, ratingsCount: 3, reviews: [review('a')] });

      expect(query('reviews-average')?.textContent?.trim()).toBe('4.3');
    });

    it('Given_SingleReview_When_Rendering_Then_UsesSingularLabel', () => {
      render({ averageRating: 5, ratingsCount: 1, reviews: [review('a')] });

      expect(query('reviews-count')?.textContent?.trim()).toBe('1 review');
    });

    it('Given_Summary_When_Rendering_Then_ExposesAccessibleScoreDescriptionAndFilledStars', () => {
      render({ averageRating: 4, ratingsCount: 2, reviews: [review('a')] });

      const summary = fixture.nativeElement.querySelector('.score-details') as HTMLElement;
      expect(summary.getAttribute('role')).toBe('img');
      expect(summary.getAttribute('aria-label')).toBe('4.0 out of 5, 2 reviews');
      expect(summary.querySelectorAll('.star-filled').length).toBe(4);
    });
  });

  describe('Distribution', () => {
    const distribution = { '1': 0, '2': 0, '3': 1, '4': 1, '5': 2 };

    it('Given_Distribution_When_Rendering_Then_ListsFiveToOneStarRowsWithPercentages', () => {
      render({ averageRating: 4.0, ratingsCount: 4, distribution, reviews: [review('a')] });

      const labels = queryAll('distribution-row').map((row) => row.querySelector('.distribution-label')?.textContent?.trim());
      expect(labels).toEqual(['5 stars', '4 stars', '3 stars', '2 stars', '1 star']);
      expect(queryAll('distribution-percent').map((el) => el.textContent?.trim())).toEqual(['50%', '25%', '25%', '0%', '0%']);
    });

    it('Given_Distribution_When_Rendering_Then_BarWidthsMatchPercentages', () => {
      render({ averageRating: 4.0, ratingsCount: 4, distribution, reviews: [review('a')] });

      const widths = Array.from(fixture.nativeElement.querySelectorAll('.distribution-fill') as NodeListOf<HTMLElement>).map(
        (bar) => bar.style.width
      );
      expect(widths).toEqual(['50%', '25%', '25%', '0%', '0%']);
    });

    it('Given_CountsThatDoNotDivideEvenly_When_Rendering_Then_RoundsPercentagesToWholeNumbers', () => {
      render({ averageRating: 4.7, ratingsCount: 3, distribution: { '5': 2, '4': 1 }, reviews: [review('a')] });

      expect(queryAll('distribution-percent').map((el) => el.textContent?.trim())).toEqual(['67%', '33%', '0%', '0%', '0%']);
    });

    it('Given_MissingDistribution_When_Rendering_Then_ShowsZeroPercentRows', () => {
      render({ averageRating: 5, ratingsCount: 2, distribution: null, reviews: [review('a')] });

      expect(queryAll('distribution-percent').map((el) => el.textContent?.trim())).toEqual(['0%', '0%', '0%', '0%', '0%']);
    });

    it('Given_Distribution_When_ComputingBreakdown_Then_ExposesCountsPerStar', () => {
      render({ averageRating: 4.0, ratingsCount: 4, distribution, reviews: [review('a')] });

      expect(fixture.componentInstance.breakdown()).toEqual([
        { star: 5, count: 2, percent: 50 },
        { star: 4, count: 1, percent: 25 },
        { star: 3, count: 1, percent: 25 },
        { star: 2, count: 0, percent: 0 },
        { star: 1, count: 0, percent: 0 }
      ]);
    });
  });

  describe('Review list', () => {
    it('Given_Reviews_When_Rendering_Then_ListsThemInTheGivenOrderWithAuthorScoreAndComment', () => {
      render({
        averageRating: 4,
        ratingsCount: 2,
        reviews: [
          review('newer', { studentName: 'Ana Pop', score: 5, comment: 'Excellent' }),
          review('older', { studentName: 'Vlad Enache', score: 3, comment: 'Okay' })
        ]
      });

      const items = queryAll('review-item');
      expect(items.length).toBe(2);
      expect(items[0].querySelector('.review-author')?.textContent?.trim()).toBe('Ana Pop');
      expect(items[0].querySelector('[data-testid="review-score"]')?.textContent?.trim()).toBe('5/5');
      expect(items[0].querySelector('[data-testid="review-comment"]')?.textContent?.trim()).toBe('Excellent');
      expect(items[1].querySelector('.review-author')?.textContent?.trim()).toBe('Vlad Enache');
      expect(items[1].querySelector('[data-testid="review-score"]')?.textContent?.trim()).toBe('3/5');
    });

    it('Given_Review_When_Rendering_Then_ShowsSessionDateAndFilledStarsForTheScore', () => {
      render({ averageRating: 3, ratingsCount: 1, reviews: [review('a', { score: 3, date: '2026-11-05T10:00:00Z' })] });

      const item = queryAll('review-item')[0];
      expect(item.querySelector('time')?.textContent).toContain('Nov 5, 2026');
      expect(item.querySelector('time')?.getAttribute('datetime')).toBe('2026-11-05T10:00:00Z');
      expect(item.querySelectorAll('.star-filled').length).toBe(3);
      expect(item.querySelector('[role="img"]')?.getAttribute('aria-label')).toBe('Rated 3 out of 5');
    });

    it.each([null, undefined, ''])('Given_ReviewWithoutName_%s_When_Rendering_Then_ShowsAnonymousStudent', (name) => {
      render({ averageRating: 5, ratingsCount: 1, reviews: [review('a', { studentName: name })] });

      expect(queryAll('review-item')[0].querySelector('.review-author')?.textContent?.trim()).toBe('Anonymous student');
    });

    it.each([null, undefined, ''])('Given_ReviewWithoutComment_%s_When_Rendering_Then_OmitsCommentBlock', (comment) => {
      render({ averageRating: 5, ratingsCount: 1, reviews: [review('a', { comment })] });

      expect(query('review-comment')).toBeNull();
      expect(queryAll('review-item').length).toBe(1);
    });

    it('Given_CommentWithMarkup_When_Rendering_Then_ShowsItAsPlainText', () => {
      render({ averageRating: 5, ratingsCount: 1, reviews: [review('a', { comment: '<b>bold</b>' })] });

      expect(query('review-comment')?.textContent?.trim()).toBe('<b>bold</b>');
      expect(query('review-comment')?.querySelector('b')).toBeNull();
    });
  });

  describe('Truncated list', () => {
    it('Given_MoreRatingsThanReviewsShown_When_Rendering_Then_ExplainsOnlyRecentReviewsAreListed', () => {
      render({ averageRating: 4.5, ratingsCount: 25, reviews: [review('a'), review('b')] });

      expect(query('reviews-truncated')?.textContent?.trim()).toBe('Showing the 2 most recent of 25 reviews.');
    });

    it('Given_AllReviewsShown_When_Rendering_Then_HidesTruncationNote', () => {
      render({ averageRating: 5, ratingsCount: 2, reviews: [review('a'), review('b')] });

      expect(query('reviews-truncated')).toBeNull();
    });
  });
});