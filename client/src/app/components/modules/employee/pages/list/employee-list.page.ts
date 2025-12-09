import { AsyncPipe, SlicePipe, DatePipe, CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';

import { EmployeeService } from '../../services/employee.service';
import { EmployeeDetailModel } from '../../models/employee.models';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
    DatePipe,
    CurrencyPipe,
  ],
  templateUrl: './employee-list.page.html',
})
export class EmployeeListPage {
  protected readonly route = inject(ActivatedRoute);
  protected readonly employeeService = inject(EmployeeService);

  protected readonly employees$ = this.route.data.pipe(
    filter((data) => data['employees']),
    map((data) => data['employees'] as EmployeeDetailModel[]),
  );
}
