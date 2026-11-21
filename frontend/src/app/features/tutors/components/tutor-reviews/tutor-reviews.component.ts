import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TutorReview } from '../../../../core/models/tutor.models';
import { StarRatingComponent } from '../star-rating/star-rating.component';

const STAR_LEVELS = [5, 4, 3, 2, 1];

export interface DistributionRow {
  star: number;
  count: number;
  percent: number;
}

@Component({
  selector: 'app-tutor-reviews',
  standalone: true,
  imports: [DatePipe, StarRatingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tutor-reviews.component.html',
  styleUrl: './tutor-reviews.component.css'
})
export class TutorReviewsComponent {
  readonly averageRating = input(0);
  readonly ratingsCount = input(0);
  readonly distribution = input<Record<string, number> | null | undefined>(null);
  readonly reviews = input<TutorReview[] | null | undefined>([]);

  readonly hasReviews = computed(() => this.ratingsCount() > 0);
  readonly reviewList = computed(() => this.reviews() ?? []);
  readonly isTruncated = computed(() => this.reviewList().length < this.ratingsCount());
  readonly countLabel = computed(() => `${this.ratingsCount()} ${this.ratingsCount() === 1 ? 'review' : 'reviews'}`);

  /** Share of ratings per star level, from 5 stars down to 1. */
  readonly breakdown = computed<DistributionRow[]>(() => {
    const total = this.ratingsCount();
    const counts = this.distribution() ?? {};
    return STAR_LEVELS.map((star) => {
      const count = counts[String(star)] ?? 0;
      return { star, count, percent: total > 0 ? Math.round((count / total) * 100) : 0 };
    });
  });
}