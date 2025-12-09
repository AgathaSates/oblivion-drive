import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { EmployeeService } from '../../services/employee.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  DeleteEmployeeByCompanyResponseModel,
  EmployeeDetailModel,
} from '../../models/employee.models';

@Component({
  selector: 'app-employee-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './employee-delete.page.html',
})
export class EmployeeDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly employeeService = inject(EmployeeService);
  private readonly notificationService = inject(NotificationService);

  protected readonly employee$ = this.route.data.pipe(
    filter((data) => !!data['employee']),
    map((data) => data['employee'] as EmployeeDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteEmployeeByCompanyResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Funcionário excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o funcionário.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/funcionarios']);
      },
    };

    this.employee$
      .pipe(
        take(1),
        switchMap((employee) => this.employeeService.deleteEmployeeByCompany(employee.id)),
      )
      .subscribe(deleteObserver);
  }
  public goBack(): void {
    this.router.navigate(['/funcionarios']);
  }
}
