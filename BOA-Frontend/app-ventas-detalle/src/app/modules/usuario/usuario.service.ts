import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, Observable, of } from 'rxjs';
import { User } from '../../interfaces/user.interface';
import { environment } from '../../../environments/environment.development';

const httpOptions = (token: string) => ({
  headers: new HttpHeaders({
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  }),
});

@Injectable({
  providedIn: 'root',
})
export class UsuarioService {
  private apiUrl = `${environment.URL_SERVICIOS}/usuario`;

  constructor(private http: HttpClient) {}

  // Id del usuario logueado (para registrar en bitácora quién hizo la acción).
  private getAdminId(): number | undefined {
    try {
      const user = JSON.parse(sessionStorage.getItem('user') || '{}');
      return user?.userId;
    } catch {
      return undefined;
    }
  }

  getUsuarios(): Observable<User[]> {
    const token = sessionStorage.getItem('token');
    if (token) {
      return this.http
        .get<User[]>(this.apiUrl, httpOptions(token))
        .pipe(catchError(this.handleError('getUsuarios', [])));
    } else {
      return of([]);
    }
  }

  private handleError<T>(operation = 'operation', result?: T) {
    return (error: any): Observable<T> => {
      console.error(error);
      return of(result as T);
    };
  }

  getUsuarioById(id: number): Observable<User> {
    const token = sessionStorage.getItem('token');
    if (token) {
      return this.http
        .get<User>(`${this.apiUrl}/${id}`, httpOptions(token))
        .pipe(catchError(this.handleError<User>('getUsuarioById')));
    } else {
      console.error('No hay token disponible');
      return of({} as User);
    }
  }

  createUsuario(usuario: User): Observable<User> {
    const token = sessionStorage.getItem('token');
    if (token) {
      if (!usuario.username || !usuario.fullname) {
        console.error('Faltan datos necesarios para crear el usuario');
        return of({} as User);
      }
      return this.http
        .post<User>(this.apiUrl, usuario, httpOptions(token))
        .pipe(catchError(this.handleError('addProduct', usuario)));
    } else {
      console.error('No hay token disponible');
      return of({} as User);
    }
  }

  updateUsuario(id: number, usuario: User): Observable<User> {
    const token = sessionStorage.getItem('token');
    if (token) {
      const adminId = this.getAdminId();
      const url = adminId ? `${this.apiUrl}/${id}?adminId=${adminId}` : `${this.apiUrl}/${id}`;
      return this.http
        .put<User>(url, usuario, httpOptions(token))
        .pipe(catchError(this.handleError('updateUsuario', usuario)));
    } else {
      console.error('No hay token disponible');
      return of({} as User);
    }
  }

  deleteUsuario(id: number): Observable<void> {
    const token = sessionStorage.getItem('token');
    if (token) {
      const adminId = this.getAdminId();
      const url = adminId ? `${this.apiUrl}/${id}?adminId=${adminId}` : `${this.apiUrl}/${id}`;
      return this.http
        .delete<void>(url, httpOptions(token))
        .pipe(catchError(this.handleError<void>('deleteUsuario')));
    } else {
      console.error('No hay token disponible');
      return of(undefined);
    }
  }
}