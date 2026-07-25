import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../../environments/environment.development';

@Component({
  selector: 'app-mis-reservas',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './mis-reservas.component.html',
  styleUrl: './mis-reservas.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MisReservasComponent implements OnInit {
  reservas: any[] = [];
  cargando: boolean = true;
  clienteId: number = 0;
  reservaSeleccionada: any = null;
  errorPerfil: string = '';

  constructor(
    private http: HttpClient,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const clienteIdRaw = sessionStorage.getItem('clienteId');
    if (!clienteIdRaw) {
      this.errorPerfil = 'No se encontró tu perfil de cliente. Contacta al administrador.';
      this.cargando = false;
      this.cdr.markForCheck();
      return;
    }
    this.clienteId = Number(clienteIdRaw);
    this.load();
  }

  load(): void {
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });

    this.http.get<any[]>(
      `${environment.URL_SERVICIOS}/venta/detalle/cliente/${this.clienteId}`,
      { headers }
    ).subscribe({
      next: (data) => {
        this.reservas = data.filter(v => v.estado === 'Pendiente');
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.cargando = false;
        this.cdr.markForCheck();
      }
    });
  }

  verDetalle(reserva: any): void {
    this.reservaSeleccionada = this.reservaSeleccionada?.id === reserva.id ? null : reserva;
    this.cdr.markForCheck();
  }

  cancelarReserva(venta: any): void {
    if (!confirm(`¿Cancelar la reserva ${venta.codigo_Venta}?`)) return;

    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });

    const ventaActualizada = {
      id: venta.id,
      codigo_Venta: venta.codigo_Venta,
      programacion_Vuelo_Id: venta.programacion_Vuelo_Id,
      cliente_Id: venta.cliente_Id,
      monto_Total: venta.monto_Total,
      estado: 'Cancelada'
    };

    this.http.put(
      `${environment.URL_SERVICIOS}/venta/${venta.id}`,
      ventaActualizada,
      { headers }
    ).subscribe({
      next: () => this.load(),
      error: () => alert('Error al cancelar la reserva')
    });
  }

  buscarVuelos(): void {
    this.router.navigate(['/dashboard/cliente/buscar-vuelos']);
  }
}