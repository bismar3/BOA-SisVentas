import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../../environments/environment.development';

@Component({
  selector: 'app-pago-qr',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pago-qr.component.html',
  styleUrls: ['./pago-qr.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PagoQrComponent implements OnInit, OnDestroy {
  vuelo: any = null;
  asiento: any = null;
  precio: number = 0;
  pasajero: any = null;
  usuarioId: number = 0;

  qrUrl: string = '';
  urlPasarela: string = '';
  idTransaccion: string = '';
  ventaId: number = 0;
  codigoVenta: string = '';
  modo: string = '';

  cargando: boolean = false;
  pagado: boolean = false;
  error: string = '';
  intervalVerificacion: any = null;

  constructor(
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.vuelo = history.state.vuelo;
    this.asiento = history.state.asiento;
    this.precio = history.state.precio;
    this.pasajero = history.state.pasajero;
    this.usuarioId = history.state.usuarioId;

    if (!this.vuelo || !this.asiento || !this.pasajero) {
      this.router.navigate(['/dashboard/cliente/buscar-vuelos']);
      return;
    }

    this.registrarPago();
  }

  ngOnDestroy(): void {
    if (this.intervalVerificacion) {
      clearInterval(this.intervalVerificacion);
    }
  }

  registrarPago(): void {
    this.cargando = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    const programacionVueloId = this.vuelo.programacionId ?? this.vuelo.id;

    const body = {
      clienteId: this.usuarioId,
      programacionVueloId: programacionVueloId,
      montoTotal: this.precio,
      asientoId: this.asiento.id,
      pasajeroNombre: this.pasajero.nombre,
      pasajeroApellido: this.pasajero.apellido,
      asientos: [
        {
          concepto: `Asiento ${this.asiento.asiento?.numero || this.asiento.numero} - ${this.asiento.asiento?.tipoClase?.nombre || ''}`,
          cantidad: 1,
          costo_unitario: this.precio,
          descuento_unitario: 0
        }
      ]
    };

    this.http.post<any>(
      `${environment.URL_SERVICIOS}/pago/registrar`,
      body,
      { headers }
    ).subscribe({
      next: (response) => {
        this.ventaId = response.ventaId;
        this.codigoVenta = response.codigoVenta;
        this.idTransaccion = response.idTransaccion;
        this.qrUrl = response.qrUrl;
        this.urlPasarela = response.urlPasarela;
        this.modo = response.modo;
        this.cargando = false;

        this.intervalVerificacion = setInterval(() => {
          this.verificarPago();
        }, 10000);

        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Error al registrar el pago. Intenta de nuevo.';
        this.cargando = false;
        this.cdr.markForCheck();
      }
    });
  }

  verificarPago(): void {
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });

    this.http.get<any>(
      `${environment.URL_SERVICIOS}/pago/verificar/${this.idTransaccion}`,
      { headers }
    ).subscribe({
      next: (response) => {
        if (response.pagado) {
          // En modo simulación la venta sigue Pendiente: hay que confirmarla llamando al
          // callback (Confirmada + bitácora + evento de ingreso). En modo Libélula, el pago
          // real ya disparó el callback, así que solo cerramos la UI.
          if (this.modo === 'simulacion') {
            this.confirmarVentaSimulada(headers);
          } else {
            this.finalizarPago();
          }
        }
      },
      error: () => {}
    });
  }

  // Reutiliza el endpoint de callback para dejar la venta simulada como Confirmada.
  private confirmarVentaSimulada(headers: HttpHeaders): void {
    const url = `${environment.URL_SERVICIOS}/pago/callback`
      + `?transaction_id=${encodeURIComponent(this.codigoVenta)}`
      + `&invoice_id=${encodeURIComponent(this.idTransaccion)}&invoice_url=`;

    this.http.get<any>(url, { headers }).subscribe({
      next: () => this.finalizarPago(),
      error: () => this.finalizarPago()   // la UI igual muestra el pago como hecho
    });
  }

  private finalizarPago(): void {
    this.pagado = true;
    if (this.intervalVerificacion) {
      clearInterval(this.intervalVerificacion);
    }
    this.cdr.markForCheck();

    setTimeout(() => {
      this.router.navigate(['/dashboard/cliente/mis-compras']);
    }, 3000);
  }

  abrirPasarela(): void {
    if (this.urlPasarela) {
      window.open(this.urlPasarela, '_blank');
    }
  }

  volver(): void {
    window.history.back();
  }
}