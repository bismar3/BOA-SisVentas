import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { User } from '../../../interfaces/user.interface';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../service/auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  imports: [
    FormsModule,
    RouterModule,
    CommonModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class LoginComponent implements OnInit {
  user!: User;
  errorMessage: string | null = null;

  constructor(private router: Router, private authService: AuthService) {}

  ngOnInit() {
    this.user = {
      username: '',
      password: '',
    };
  }

  public login(): void {
    this.authService.getToken(this.user).subscribe(
      (response: User) => {
        sessionStorage.setItem("token", response.token || '');
        sessionStorage.setItem('user', JSON.stringify(response));
        sessionStorage.setItem('roles', JSON.stringify(response.roles));
        this.resolverClienteYRedirigir(response.userId);
      },
      (error) => {
        this.errorMessage = error.message;
        this.router.navigate(['/auth/login']);
      }
    );
  }

  // Resuelve y cachea la PK del cliente (Comercial) a partir del userId (Seguridad).
  // Si el cliente no existe (404 — p. ej. RabbitMQ falló al registrar), el login NO se rompe:
  // el usuario entra igual, solo que sin clienteId cacheado.
  private resolverClienteYRedirigir(userId?: number): void {
    if (!userId) {
      sessionStorage.removeItem('clienteId');
      this.redirectToDashboard();
      return;
    }
    this.authService.getClienteByUsuario(userId).subscribe({
      next: (cliente) => {
        if (cliente && cliente.id != null) {
          sessionStorage.setItem('clienteId', String(cliente.id));
        } else {
          sessionStorage.removeItem('clienteId');
        }
        this.redirectToDashboard();
      },
      error: () => {
        console.warn(`[Login] No se encontró un cliente asociado al usuario ${userId}. Se continúa sin clienteId.`);
        sessionStorage.removeItem('clienteId');
        this.redirectToDashboard();
      }
    });
  }

  private redirectToDashboard(): void {
    // Consumo "read-once": se lee y se destruye de inmediato, exista o no un vuelo
    // válido y ocurra o no un error después. Así no secuestra logins posteriores.
    const vueloPendienteRaw = sessionStorage.getItem('vuelo_pendiente');
    sessionStorage.removeItem('vuelo_pendiente');

    if (vueloPendienteRaw) {
      try {
        const vuelo = JSON.parse(vueloPendienteRaw);
        this.router.navigate(['/dashboard/cliente/seleccionar-asiento', vuelo.programacionId], {
          state: { vuelo }
        });
        return;
      } catch {
        // JSON corrupto: ya se removió arriba; caemos al dashboard.
      }
    }

    this.router.navigate(['/dashboard']);
  }
}