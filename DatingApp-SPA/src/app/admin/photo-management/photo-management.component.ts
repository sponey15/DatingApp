import { Component, OnInit, Input } from '@angular/core';
import { AdminService } from 'src/app/_services/admin.service';
import { AuthService } from 'src/app/_services/auth.service';
import { Photo } from 'src/app/_models/Photo';
import { User } from 'src/app/_models/user';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.scss']
})
export class PhotoManagementComponent implements OnInit {
  photos: Photo[];

  constructor(public authService: AuthService, private adminService: AdminService) { }

  ngOnInit() {
    this.getPhotosForModeration();
  }

  getPhotosForModeration() {
    this.adminService.getPhotosForModeration().subscribe((photos: Photo[]) => {
      this.photos = photos;
    }, error => {
      console.log(error);
    });
  }

  acceptPhoto(id: number) {
    this.adminService.acceptPhoto(id).subscribe(() => {
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
    });
  }

  rejectPhoto(id: number) {
    this.adminService.rejectPhoto(id).subscribe(() => {
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
    });
  }
}
