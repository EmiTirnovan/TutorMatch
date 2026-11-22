import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export const MAX_STARS = 5;

/** Decorative row of five stars. The parent provides the accessible text. */
@Component({
  selector: 'app-star-rating',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './star-rating.component.html',
  styleUrl: './star-rating.component.css'
})
export class StarRatingComponent {
  /** Rating from 0 to 5. Fractions are rounded to the nearest whole star. */
  readonly value = input.required<number>();
  readonly size = input(16);

  /** One entry per star, true when the star is filled. */
  readonly stars = computed(() => {
    const filled = Math.min(MAX_STARS, Math.max(0, Math.round(this.value())));
    return Array.from({ length: MAX_STARS }, (_, index) => index < filled);
  });
}