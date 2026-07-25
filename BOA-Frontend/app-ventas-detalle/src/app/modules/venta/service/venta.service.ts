import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { VentaDetalle } from '../../../interfaces/venta.interface';

@Injectable({ providedIn: 'root' })
export class VentaService {
  private apiUrl = 'http://localhost:6005/api/venta';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token') || '';
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  getAllDetalle(): Observable<VentaDetalle[]> {
    return this.http.get<VentaDetalle[]>(`${this.apiUrl}/detalle`, { headers: this.getHeaders() });
  }

  eliminarVenta(id: number, adminId?: number): Observable<any> {
    const url = adminId ? `${this.apiUrl}/${id}?adminId=${adminId}` : `${this.apiUrl}/${id}`;
    return this.http.delete(url, { headers: this.getHeaders() });
  }
}