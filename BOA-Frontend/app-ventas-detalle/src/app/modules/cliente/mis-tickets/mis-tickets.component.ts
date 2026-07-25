import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { environment } from '../../../../environments/environment.development';

@Component({
  selector: 'app-mis-tickets',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mis-tickets.component.html',
  styleUrl: './mis-tickets.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MisTicketsComponent implements OnInit {
  tickets: any[] = [];
  cargando: boolean = true;
  clienteId: number = 0;
  ticketSeleccionado: any = null;
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
      next: (ventas) => {
        const confirmadas = ventas.filter(v => v.estado === 'Confirmada' && v.numero_Ticket !== '-');

        if (confirmadas.length === 0) {
          this.cargando = false;
          this.cdr.markForCheck();
          return;
        }

        const requests = confirmadas.map(v =>
          this.http.get<any>(`${environment.URL_SERVICIOS}/programacionvuelo/${v.programacion_Vuelo_Id}`, { headers })
        );

        forkJoin(requests).subscribe({
          next: (vuelos) => {
            const aeropuertoIds = new Set<number>();
            vuelos.forEach(v => {
              aeropuertoIds.add(v.aeropuerto_Origen_Id);
              aeropuertoIds.add(v.aeropuerto_Destino_Id);
            });

            const aeropuertoRequests = Array.from(aeropuertoIds).map(id =>
              this.http.get<any>(`${environment.URL_SERVICIOS}/aeropuerto/${id}`, { headers })
            );

            forkJoin(aeropuertoRequests).subscribe({
              next: (aeropuertos) => {
                this.tickets = confirmadas.map((v, i) => {
                  const vuelo = vuelos[i];
                  const origen = aeropuertos.find(a => a.id === vuelo.aeropuerto_Origen_Id);
                  const destino = aeropuertos.find(a => a.id === vuelo.aeropuerto_Destino_Id);
                  return { ...v, vuelo, origen, destino };
                });
                this.cargando = false;
                this.cdr.markForCheck();
              }
            });
          }
        });
      },
      error: () => {
        this.cargando = false;
        this.cdr.markForCheck();
      }
    });
  }

  verBoardingPass(ticket: any): void {
    this.ticketSeleccionado = ticket;
    this.cdr.markForCheck();
  }

  cerrarModal(): void {
    this.ticketSeleccionado = null;
    this.cdr.markForCheck();
  }

  imprimir(): void {
    const contenido = document.getElementById('boarding-pass')?.innerHTML;
    if (!contenido) return;

    const ventana = window.open('', '_blank', 'width=800,height=600');
    ventana?.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>Boarding Pass - ${this.ticketSeleccionado.numero_Ticket}</title>
        <meta charset="utf-8">
        <script src="https://cdn.tailwindcss.com"><\/script>
        <style>
          body { margin: 0; padding: 20px; background: white; font-family: sans-serif; }
          @media print {
            body { padding: 0; }
            button { display: none !important; }
          }
        </style>
      </head>
      <body>
        <div>${contenido}</div>
        <script>
          window.onload = function() { window.print(); }
        <\/script>
      </body>
      </html>
    `);
    ventana?.document.close();
  }

  buscarVuelos(): void {
    this.router.navigate(['/dashboard/cliente/buscar-vuelos']);
  }
}