Implementar la pantalla "Buscar en SPs" 

---

## Layout de la pantalla

+--------------------------------------------------+
| Conexión:  [ComboBox - lista de conexiones]      |
|                                                  |
| Filtro SP: [TextBox @part_name_sp    ]           |
| Buscar texto: [TextBox @part_name_find]          |
|                                                  |
|              [ Botón: Buscar ]                   |
|                                                  |
| +----------------------------------------------+ |
| | DataGridView con resultados                  | |
| | Columnas: Store | CantOcurrencias            | |
| +----------------------------------------------+ |
+--------------------------------------------------+

---

## Componentes de la pantalla

### ComboBox – Conexiones
- Cargar al iniciar el formulario desde el archivo `conexiones.json`
- Mostrar el campo `Detalle` como texto visible
- Al seleccionar una conexión, usar sus datos para armar 
  el SqlConnection según el tipo:
    - UserPass → Server + BD + User + Password
    - IntegratedSecurity → Server + BD + Integrated Security=True
    - ConnectionString → usar directamente el string guardado

### TextBox – @part_name_sp
- Label: "Filtro nombre SP"
- Placeholder o texto de ayuda: "Dejar vacío para no filtrar"
- Valor por defecto: cadena vacía ""

### TextBox – @part_name_find
- Label: "Texto a buscar"
- Obligatorio: no permitir ejecutar la búsqueda si está vacío
- Valor por defecto: ""

### Botón – Buscar
- Validar que:
    - Haya una conexión seleccionada en el ComboBox
    - @part_name_find no esté vacío
- Ejecutar el query contra la base de datos de la conexión elegida
- Mostrar los resultados en el DataGridView
- Mostrar mensaje de error descriptivo si falla la conexión 
  o el query

### DataGridView – Resultados
- Columnas fijas:
    - `Store` (string) → valor ejecutable del tipo: 
       sp_helptext 'schema.nombre_sp'
    - `CantOcurrencias` (int)
- Ordenado por CantOcurrencias DESC (ya viene ordenado del query)
- La columna `Store` debe ser clickeable: al hacer doble clic 
  copiar el valor al portapapeles (para poder pegarlo en SSMS)
- Mostrar cantidad de resultados encontrados debajo de la grilla

---

## Query a ejecutar

Parametrizar con los valores ingresados por el usuario.
NO concatenar strings directamente: usar SqlCommand con parámetros
para @part_name_sp y @part_name_find.

El query a ejecutar es el siguiente (no modificarlo):

    declare @BD varchar(50)
    declare @part_name_sp varchar(50)
    declare @part_name_find varchar(50)

    set @part_name_sp   = @param_sp    -- viene del TextBox
    set @part_name_find = @param_find  -- viene del TextBox

    set @BD = DB_NAME()

    exec('use ' + @BD)

    select  'sp_helptext ' + '''' + s.name + '.' + o.name + '''' as Store,
            COUNT(*) as CantOcurrencias
    from        sysobjects o
    inner join  syscomments c on o.id = c.id
    inner join  sys.schemas s on s.schema_id = o.uid
    where
        o.name like '%' + ltrim(rtrim(@part_name_sp)) + '%'
        and (type = 'p' or type = 'v')
        and text like '%' + ltrim(rtrim(@part_name_find)) + '%'
    group by o.name, s.name
    order by 2 desc

---

## Requisitos técnicos

- Ejecutar el query de forma asíncrona (async/await) para no 
  bloquear la UI durante la búsqueda
- Mostrar cursor de espera (Cursor.Wait) mientras se ejecuta
- Deshabilitar el botón Buscar mientras se está ejecutando 
  para evitar doble ejecución
- Compatible con SQL Server (MSSQL) únicamente, ya que el query 
  usa sysobjects y syscomments que son objetos del sistema de MSSQL

---

## Manejo de errores

Contemplar y mostrar mensajes claros para los siguientes casos:
- No se pudo conectar al servidor (timeout, credenciales incorrectas)
- La base de datos no existe o no tiene permisos
- El query no devuelve resultados (mostrar "Sin resultados" 
  en la grilla)
- Cualquier SqlException inesperada

---

## Extras opcionales

- Botón "Limpiar" para resetear los filtros y vaciar la grilla
- Botón "Exportar a CSV" con los resultados de la grilla
- Tooltip en la columna Store explicando que doble clic 
  copia al portapapeles