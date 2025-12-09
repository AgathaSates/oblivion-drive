import { AsyncPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { filter, map } from 'rxjs';
import { PartnerDetailModel } from '../../models/partner.models';

@Component({
  selector: 'app-partner-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
  ],
  templateUrl: './partner-list.page.html',
})
export class PartnerListPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly partners$ = this.route.data.pipe(
    filter((data) => data['partners']),
    map((data) => data['partners'] as PartnerDetailModel[]),
  );
}
