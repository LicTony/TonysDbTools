# Plan de Implementación del Provider Oracle para TonysDbTools

Este documento detalla los pasos necesarios para implementar completamente el soporte de Oracle en `OracleMetadataProvider`. Cada paso incluye una fase de validación para asegurar la calidad de la implementación.

## Prerrequisitos
- Agregar el paquete NuGet `Oracle.ManagedDataAccess` (versión compatible con .NET 10.0-windows).
- Contar con un ambiente de base de datos Oracle para pruebas.

---

## Paso 1: Conectividad Básica y Prueba de Conexión
**Objetivo:** Establecer la comunicación inicial con Oracle.

1.  **Dependencias:** Ejecutar `dotnet add package Oracle.ManagedDataAccess`.
2.  **Referencia:** Importar `using Oracle.ManagedDataAccess.Client;` en `OracleMetadataProvider.cs`.
3.  **Implementación:**
    - Completar `TestConnectionAsync()`:
      ```csharp
      using var connection = new OracleConnection(_connectionString);
      await connection.OpenAsync();
      return true;
      ```
4.  **Validación:**
    - Abrir la pantalla de **Conexiones**.
    - Crear una conexión de tipo Oracle.
    - Presionar el botón de **Probar Conexión**.
    - Confirmar que el mensaje sea "Conexión exitosa".

---

## Paso 2: Listado de Tablas
**Objetivo:** Obtener todas las tablas accesibles por el usuario.

1.  **Consulta SQL:** Usar `ALL_TABLES` (o `USER_TABLES` si solo se quieren las propias).
2.  **Implementación:** Completar `GetAllTablesAsync()`.
    - SQL Sugerido: `SELECT OWNER, TABLE_NAME FROM ALL_TABLES ORDER BY OWNER, TABLE_NAME`
    - Formato de retorno: `OWNER.TABLE_NAME`.
3.  **Validación:**
    - Navegar a la pantalla de **Relación entre 2 Tablas**.
    - Verificar que los ComboBoxes se llenen con la lista de tablas de Oracle.

---

## Paso 3: Relaciones de Claves Foráneas (FK)
**Objetivo:** Identificar las relaciones explícitas entre tablas para el grafo de joins.

1.  **Consultas SQL:** Combinar `ALL_CONSTRAINTS` (para el tipo de constraint 'R') y `ALL_CONS_COLUMNS`.
2.  **Implementación:** Completar `GetForeignKeyRelationsAsync()`.
    - Debe mapear los campos a la clase `ForeignKeyRelation`.
3.  **Validación:**
    - En la pantalla de **Relación entre 2 Tablas**, seleccionar dos tablas que tengan una FK conocida.
    - Presionar **Buscar Caminos**.
    - Verificar que el sistema encuentre el camino utilizando las relaciones FK.

---

## Paso 4: Búsqueda de Joins Implícitos
**Objetivo:** Encontrar relaciones basadas en nombres de columnas idénticos (muy común en sistemas antiguos).

1.  **Consulta SQL:** Usar `ALL_TAB_COLUMNS`.
2.  **Implementación:** Completar `FindImplicitJoinsAsync(string[] tableNames)`.
    - Buscar columnas con el mismo nombre y tipo de dato entre las tablas seleccionadas.
3.  **Validación:**
    - Seleccionar dos tablas sin FK explícita pero con campos comunes (ej: `ID_CLIENTE`).
    - Verificar que se sugiera la relación implícita.

---

## Paso 5: Búsqueda en Stored Procedures (SPs)
**Objetivo:** Buscar texto dentro del código fuente de procedimientos, funciones y paquetes.

1.  **Consulta SQL:** Usar `ALL_SOURCE`.
    - SQL Sugerido para búsqueda:
      ```sql
      SELECT OWNER || '.' || NAME AS Store, COUNT(*) AS CantOcurrencias
      FROM ALL_SOURCE
      WHERE (TYPE = 'PROCEDURE' OR TYPE = 'PACKAGE BODY' OR TYPE = 'FUNCTION')
        AND (LOWER(NAME) LIKE LOWER('%' || :p_name || '%'))
        AND (LOWER(TEXT) LIKE LOWER('%' || :p_text || '%'))
      GROUP BY OWNER, NAME, TYPE
      ORDER BY 2 DESC
      ```
2.  **Implementación:**
    - Completar `SearchInSpsAsync`.
    - Completar `GetSpCodeAsync` (concatenando todas las líneas de `ALL_SOURCE` para ese objeto).
3.  **Validación:**
    - Navegar a **Buscar en SPs**.
    - Realizar una búsqueda de un término conocido.
    - Al hacer doble clic, verificar que se abra el detalle con el código completo.

---

## Paso 6: Búsqueda de Datos (Texto y Números)
**Objetivo:** Localizar registros que contengan un valor específico en cualquier columna de la base de datos.

1.  **Estrategia:** Generar SQL dinámico similar a MSSQL pero adaptado a la sintaxis PL/SQL (usar `EXECUTE IMMEDIATE`).
2.  **Implementación:**
    - Completar `SearchTextInTablesAsync`.
    - Completar `SearchNumberInTablesAsync`.
    - Limitar los resultados por tabla usando `FETCH FIRST n ROWS ONLY` (Oracle 12c+) o `ROWNUM`.
3.  **Validación:**
    - Navegar a **Buscar Texto**.
    - Buscar una cadena que se sepa que existe.
    - Verificar que se muestre la tabla, columna y el valor encontrado.
    - Repetir para **Buscar Número**.

---

## Consideraciones de Rendimiento
- Oracle puede ser lento al consultar `ALL_TAB_COLUMNS` en bases de datos muy grandes. Considerar usar `USER_TAB_COLUMNS` si el usuario no tiene permisos sobre todo el esquema.
- El timeout de los comandos debe ser generoso (300 segundos) para las búsquedas de texto globales.
