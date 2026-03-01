Diseño de la pagina Conexiones

implementa un CRUD completo 
para gestionar "cadenas de conexión a bases de datos".

Los datos deben persistirse en un archivo local llamado `conexiones.json`.

---

## Modelo de datos

Cada conexión tiene un campo común:
- `Id` (int, autogenerado)
- `Tipo` (enum UserPass, IntegratedSecurity y ConnectionString): define la estructura del resto de los campos
- `Detalle` (string): descripción libre de la conexión

Según el valor de `Tipo`, los campos adicionales varían:

### Tipo 1 – "UserPass" (conexión con usuario y contraseña)
- Server (string)
- BaseDeDatos (string)
- Usuario (string)
- Password (string)

### Tipo 2 – "IntegratedSecurity" (autenticación de Windows)
- Server (string)
- BaseDeDatos (string)

### Tipo 3 – "ConnectionString" (cadena de conexión directa)
- ConnectionString (string)

---

## Funcionalidades requeridas (CRUD)

- **Crear**: solicitar al usuario el tipo de conexión y completar 
  solo los campos correspondientes
- **Leer**: listar todas las conexiones almacenadas (mostrar todos 
  los campos relevantes según el tipo)
- **Actualizar**: buscar por Id y permitir editar los campos
- **Eliminar**: buscar por Id y eliminar la conexión

---

## Requisitos técnicos

- Usar herencia o discriminated union para modelar los 3 tipos 
  (clase base `Conexion` con subclases o propiedad polimórfica)
- Serializar/deserializar con `System.Text.Json` o `Newtonsoft.Json`
  (manejar correctamente el polimorfismo en la serialización)
- Menú interactivo en consola con opciones numeradas
- Validar que los campos obligatorios no estén vacíos
- El archivo JSON debe crearse automáticamente si no existe

---

## Extras opcionales (si querés más desafío)

- Enmascarar el password al ingresarlo (mostrar asteriscos)
- Agregar una opción para "probar" la conexión usando 
  `SqlConnection` o similar
- Exportar una conexión como string listo para usar en un 
  `appsettings.json`