import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VentaService } from '../service/venta.service';
import { VentaDetalle } from '../../../interfaces/venta.interface';

@Component({
  selector: 'app-ventas-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ventas-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VentasListComponent implements OnInit {
  ventas: VentaDetalle[] = [];
  ventasFiltradas: VentaDetalle[] = [];

  filtroTexto: string = '';
  filtroEstado: string = 'Todos';
  filtroFechaDesde: string = '';
  filtroFechaHasta: string = '';

  constructor(
    private ventaService: VentaService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.ventaService.getAllDetalle().subscribe(data => {
      this.ventas = data;
      this.ventasFiltradas = data;
      this.cdr.markForCheck();
    });
  }

  get totalRecaudado(): number {
    return this.ventas
      .filter(v => v.estado === 'Confirmada')
      .reduce((sum, v) => sum + v.monto_Total, 0);
  }

  get totalConfirmadas(): number {
    return this.ventas.filter(v => v.estado === 'Confirmada').length;
  }

  get totalCanceladas(): number {
    return this.ventas.filter(v => v.estado === 'Cancelada').length;
  }

  filtrar(): void {
    const texto = this.filtroTexto.toLowerCase();
    this.ventasFiltradas = this.ventas.filter(v => {
      const coincideTexto = !texto ||
        v.codigo_Venta.toLowerCase().includes(texto) ||
        v.cliente_Nombre.toLowerCase().includes(texto) ||
        v.cliente_Documento.includes(texto) ||
        v.programacion_Vuelo_Id.toString().includes(texto);

      const coincideEstado = this.filtroEstado === 'Todos' || v.estado === this.filtroEstado;

      return coincideTexto && coincideEstado;
    });
    this.cdr.markForCheck();
  }

  limpiar(): void {
    this.filtroTexto = '';
    this.filtroEstado = 'Todos';
    this.ventasFiltradas = this.ventas;
    this.cdr.markForCheck();
  }

  getEstadoClass(estado: string): string {
    switch (estado) {
      case 'Confirmada': return 'bg-green-600';
      case 'Cancelada': return 'bg-red-600';
      case 'Pendiente': return 'bg-yellow-500';
      default: return 'bg-gray-600';
    }
  }

  eliminar(v: VentaDetalle): void {
    if (!confirm(`¿Eliminar la venta ${v.codigo_Venta}? Esta acción no se puede deshacer.`)) return;

    this.ventaService.eliminarVenta(v.id, this.getAdminId()).subscribe({
      next: () => {
        this.ventas = this.ventas.filter(x => x.id !== v.id);
        this.ventasFiltradas = this.ventasFiltradas.filter(x => x.id !== v.id);
        this.cdr.markForCheck();
      },
      error: (e) => {
        console.error('[Ventas] Error al eliminar', e);
        alert('No se pudo eliminar la venta.');
      }
    });
  }

  // Id del usuario admin logueado (para registrar en bitácora quién borró).
  private getAdminId(): number | undefined {
    try {
      const user = JSON.parse(sessionStorage.getItem('user') || '{}');
      return user?.userId;
    } catch {
      return undefined;
    }
  }

  getMetodoPagoClass(metodo: string): string {
    switch (metodo) {
      case 'QR': return 'bg-cyan-600';
      case 'Tarjeta': return 'bg-orange-500';
      default: return 'bg-gray-500';
    }
  }
}