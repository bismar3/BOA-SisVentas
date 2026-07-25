import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { routes } from '../../../app.routes';
import { SidebarComponent } from "../../components/sidebar/sidebar.component";
import { Rol } from '../../../interfaces/rol.interface';
import { Permiso } from '../../../interfaces/permiso.interface';
import { User } from '../../../interfaces/user.interface';
import { ThemeService } from '../../services/theme.service';
import { forkJoin } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { ProgramacionVueloService } from '../../../modules/programacion-vuelo/service/programacion-vuelo.service';
import { UsuarioService } from '../../../modules/usuario/usuario.service';
import { AeropuertoService } from '../../../modules/aeropuerto/service/aeropuerto.service';
import { RutaService } from '../../../modules/ruta/service/ruta.service';

interface ResultadoBusqueda {
  tipo: 'Vuelo' | 'Usuario' | 'Aeropuerto' | 'Ruta';
  icono: string;
  titulo: string;
  subtitulo: string;
  ruta: string[];
}

@Component({
  selector: 'app-dashboard-layout',
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    SidebarComponent
  ],
  templateUrl: './dashboardLayout.component.html',
  styleUrl: './dashboardLayout.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardLayoutComponent implements OnInit, AfterViewInit {

  public userPermissions: Permiso[] = [];
  user: User | undefined;

  mostrarMenuUsuario: boolean = false;

  searchTerm: string = '';
  resultadosBusqueda: ResultadoBusqueda[] = [];
  buscandoGlobal: boolean = false;
  private debounceTimer: any;

  constructor(
    private router: Router,
    public themeService: ThemeService,
    private cdr: ChangeDetectorRef,
    private programacionVueloService: ProgramacionVueloService,
    private usuarioService: UsuarioService,
    private aeropuertoService: AeropuertoService,
    private rutaService: RutaService
  ) {}

  ngOnInit(): void {
    const permissions = JSON.parse(sessionStorage.getItem('roles') || '[]');
    const user = JSON.parse(sessionStorage.getItem('user') || '[]');
    this.user = user;
    this.userPermissions = permissions.flatMap((rol: Rol) => rol.permisos);
  }

  ngAfterViewInit(): void {
    // El filtro de tema necesita que el DOM (#theme-root) ya exista
    setTimeout(() => this.themeService.applyTheme(), 0);
  }

  public routes = routes.find(r => r.path === 'dashboard')?.children?.filter(route => route.data);

  hasPermission(permissionName: string): boolean {
    return this.userPermissions.some(perm => perm.nombre_Permiso === permissionName);
  }

  logout() {
    // Limpia TODO lo que sea del usuario para que nada sobreviva de una sesión a la siguiente.
    sessionStorage.removeItem('user');
    sessionStorage.removeItem('roles');
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('clienteId');
    sessionStorage.removeItem('vuelo_pendiente');
    this.router.navigate(['/']);
  }

  toggleMenuUsuario(): void {
    this.mostrarMenuUsuario = !this.mostrarMenuUsuario;
  }

  cerrarMenuUsuario(): void {
    this.mostrarMenuUsuario = false;
  }

  setColor(color: 'blue' | 'orange' | 'green'): void {
    this.themeService.setColor(color);
  }

  toggleModo(): void {
    this.themeService.toggleMode();
  }

  onBuscarGlobalInput(): void {
    clearTimeout(this.debounceTimer);

    if (this.searchTerm.trim().length < 2) {
      this.resultadosBusqueda = [];
      return;
    }

    this.debounceTimer = setTimeout(() => {
      this.buscarGlobal();
    }, 400);
  }

  private buscarGlobal(): void {
    const texto = this.searchTerm.toLowerCase();
    this.buscandoGlobal = true;
    this.cdr.markForCheck();

    // Un usuario es "privilegiado" si administra alguna de las fuentes que expone el buscador.
    // El Cliente (solo compras/reservas/tickets) no las tiene: solo ve vuelos, con enlace a comprar.
    const esPrivilegiado = ['usuarios', 'aeropuertos', 'rutas', 'programacion_vuelos']
      .some(p => this.hasPermission(p));

    if (!esPrivilegiado) {
      this.programacionVueloService.getAll().subscribe(vuelos => {
        const resultados: ResultadoBusqueda[] = [];
        vuelos
          .filter(v => v.codigo_Vuelo?.toLowerCase().includes(texto))
          .slice(0, 5)
          .forEach(v => resultados.push({
            tipo: 'Vuelo',
            icono: '✈️',
            titulo: v.codigo_Vuelo,
            subtitulo: `Estado: ${v.estado}`,
            ruta: ['/dashboard/cliente/buscar-vuelos']
          }));

        this.resultadosBusqueda = resultados;
        this.buscandoGlobal = false;
        this.cdr.markForCheck();
      });
      return;
    }

    forkJoin({
      vuelos: this.programacionVueloService.getAll(),
      usuarios: this.usuarioService.getUsuarios(),
      aeropuertos: this.aeropuertoService.getAll(),
      rutas: this.rutaService.getAll()
    }).subscribe(({ vuelos, usuarios, aeropuertos, rutas }) => {
      const resultados: ResultadoBusqueda[] = [];

      vuelos
        .filter(v => v.codigo_Vuelo?.toLowerCase().includes(texto))
        .slice(0, 3)
        .forEach(v => resultados.push({
          tipo: 'Vuelo',
          icono: '✈️',
          titulo: v.codigo_Vuelo,
          subtitulo: `Estado: ${v.estado}`,
          ruta: ['/dashboard/programacion-vuelo/view', String(v.id)]
        }));

      usuarios
        .filter(u => u.username?.toLowerCase().includes(texto) || u.fullname?.toLowerCase().includes(texto))
        .slice(0, 3)
        .forEach(u => resultados.push({
          tipo: 'Usuario',
          icono: '👤',
          titulo: u.fullname || u.username,
          subtitulo: `@${u.username}`,
          ruta: ['/dashboard/user/edit', String(u.userId)]
        }));

      aeropuertos
        .filter(a => a.ciudad?.toLowerCase().includes(texto) || a.codigo_IATA?.toLowerCase().includes(texto) || a.nombre?.toLowerCase().includes(texto))
        .slice(0, 3)
        .forEach(a => resultados.push({
          tipo: 'Aeropuerto',
          icono: '📍',
          titulo: `${a.ciudad} (${a.codigo_IATA})`,
          subtitulo: a.nombre,
          ruta: ['/dashboard/aeropuerto/edit', String(a.id)]
        }));

      rutas
        .filter(r => r.tipo?.toLowerCase().includes(texto))
        .slice(0, 3)
        .forEach(r => resultados.push({
          tipo: 'Ruta',
          icono: '🛣️',
          titulo: `Ruta #${r.id}`,
          subtitulo: r.tipo,
          ruta: ['/dashboard/ruta/list']
        }));

      this.resultadosBusqueda = resultados;
      this.buscandoGlobal = false;
      this.cdr.markForCheck();
    });
  }

  irAResultado(resultado: ResultadoBusqueda): void {
    this.router.navigate(resultado.ruta);
    this.searchTerm = '';
    this.resultadosBusqueda = [];
  }
}