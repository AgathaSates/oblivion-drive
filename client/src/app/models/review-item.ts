export interface ReviewItem {
  readonly id: number;
  readonly name: string;
  readonly avatarUrl: string;
  readonly rating: number;
  readonly text: string;
  readonly row: 'top' | 'bottom';
}
