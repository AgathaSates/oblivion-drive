import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { ReviewItem } from '../../../../models/review-item';

@Component({
  selector: 'as-review-card',
  imports: [MatIconModule],
  templateUrl: './as-review-card.html',
  styleUrl: './as-review-card.scss',
})
export class AsReviewCard {
  @Input({ required: true })
  review!: ReviewItem;

  get fullStars(): number[] {
    const integerPart = Math.floor(this.review.rating);
    return Array.from({ length: integerPart });
  }

  get hasHalfStar(): boolean {
    return this.review.rating % 1 >= 0.5;
  }
}
