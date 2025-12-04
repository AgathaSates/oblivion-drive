import { Component } from '@angular/core';
import { ReviewItem } from '../../../../models/review-item';
import { REVIEW_ITEMS } from '../../../../data/review-tems.data';
import { AsReviewCard } from '../as-review-card/as-review-card';

@Component({
  selector: 'as-reviews',
  imports: [AsReviewCard],
  templateUrl: './as-reviews.html',
  styleUrl: './as-reviews.scss',
})
export class AsReviews {
  private readonly allReviews: readonly ReviewItem[] = REVIEW_ITEMS;

  readonly reviewsTop: readonly ReviewItem[] = this.allReviews.filter(
    (review) => review.row === 'top',
  );

  readonly reviewsBottom: readonly ReviewItem[] = this.allReviews.filter(
    (review) => review.row === 'bottom',
  );
}
