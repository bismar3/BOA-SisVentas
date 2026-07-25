import { Rol } from "./rol.interface";

export interface User {
  userId?: number;
  fullname?: string;
  username: string;
  password?: string;
  email?: string;
  nombre?: string;
  apellido?: string;
  rol_Id?: number;
  estado?: string;
  documento_Identidad?: string;
  telefono?: string;
  fecha_Nacimiento?: string;
  roles?: Rol[];
  fecha_Creacion?: string;
  direccion?: string;
  token?: string;
}