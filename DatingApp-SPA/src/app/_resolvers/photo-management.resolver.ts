import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, Router } from '@angular/router';
import { User } from '../_models/user';
import { UserService } from '../_services/user.service';
import { AlertifyService } from '../_services/alertify.service';
import { pipe, of, Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../_services/auth.service';
import { Photo } from '../_models/Photo';
import { AdminService } from '../_services/admin.service';

@Injectable()
export class PhotoManagementResolver implements Resolve<Photo> {
    constructor(private adminService: AdminService, private authService: AuthService,
                private router: Router, private alertify: AlertifyService) {}

    resolve(route: ActivatedRouteSnapshot): Observable<Photo> {
        return this.adminService.getPhotosForModeration().pipe(
            catchError(error => {
                this.alertify.error('Problem retriving data');
                this.router.navigate(['/admin']);
                return of(null);
            })
        );
    }
}
