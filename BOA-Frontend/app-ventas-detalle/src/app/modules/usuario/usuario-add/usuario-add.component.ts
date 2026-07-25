import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsuarioService } from '../usuario.service';
import { RolService } from '../../rol/rol.service';
import { Rol } from '../../../interfaces/rol.interface';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-usuario-add',
  imports: [FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './usuario-add.component.html',
  styleUrl: './usuario-add.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsuarioAddComponent {
  userForm!: FormGroup;
  roles: Rol[] = [];

  constructor(
    private fb: FormBuilder,
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userForm = this.fb.group({
      username: ['', [Validators.required, Validators.maxLength(30)]],
      nombre: ['', [Validators.required, Validators.maxLength(30)]],
      apellido: ['', [Validators.required, Validators.maxLength(30)]],
      direccion: [''],
      password: ['', [Validators.required, Validators.maxLength(30)]],
      email: ['', [Validators.email]],
      documento_Identidad: [''],
      telefono: [''],
      fecha_Nacimiento: [''],
      rol_Id: [null, [Validators.required]],
      estado: ['Activo'],
    });

    this.rolService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
        this.cdr.markForCheck();
      }
    });
  }

  createUser(): void {
  if (this.userForm.valid) {
    const v = this.userForm.value;
    const user = {
      ...v,
      fullname: `${v.nombre} ${v.apellido}`.trim(),
      rol_Id: Number(v.rol_Id),
      fecha_Nacimiento: v.fecha_Nacimiento ? v.fecha_Nacimiento : null,
    };

    this.usuarioService.createUsuario(user).subscribe({
      // ... resto igual, sin cambios
    });
  } else {
    console.log('Formulario inválido');
  }
}

  goBack() {
    this.router.navigate(['/dashboard/user']);
  }
}