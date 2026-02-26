-- ============================================
-- Script de Índices para Mejorar Rendimiento
-- PrenominaApi Database
-- ============================================

-- IMPORTANTE: Ejecutar en horario de baja actividad
-- Algunos índices pueden tomar tiempo en tablas grandes

USE [PrenominaApi];
GO

-- ============================================
-- Tabla: assistance_incidents
-- Queries frecuentes: búsqueda por empleado, fecha, empresa
-- ============================================

-- Índice compuesto para búsquedas por empleado y rango de fechas
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AssistanceIncidents_Employee_Date' AND object_id = OBJECT_ID('assistance_incidents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AssistanceIncidents_Employee_Date
    ON assistance_incidents (EmployeeCode, Date)
    INCLUDE (CompanyId, IncidentCode, Approved, TimeOffRequest, UpdatedAt);

    PRINT 'Created index: IX_AssistanceIncidents_Employee_Date';
END
GO

-- Índice para búsquedas por empresa y rango de fechas
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AssistanceIncidents_Company_Date' AND object_id = OBJECT_ID('assistance_incidents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AssistanceIncidents_Company_Date
    ON assistance_incidents (CompanyId, Date)
    INCLUDE (EmployeeCode, IncidentCode, Approved);

    PRINT 'Created index: IX_AssistanceIncidents_Company_Date';
END
GO

-- Índice para búsqueda por código de incidencia
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AssistanceIncidents_IncidentCode' AND object_id = OBJECT_ID('assistance_incidents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AssistanceIncidents_IncidentCode
    ON assistance_incidents (IncidentCode)
    INCLUDE (EmployeeCode, Date, Approved);

    PRINT 'Created index: IX_AssistanceIncidents_IncidentCode';
END
GO

-- ============================================
-- Tabla: employee_check_ins
-- Queries frecuentes: búsqueda por empleado, fecha, empresa
-- ============================================

-- Índice compuesto para búsquedas por empleado y rango de fechas
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeCheckIns_Employee_Date' AND object_id = OBJECT_ID('employee_check_ins'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeCheckIns_Employee_Date
    ON employee_check_ins (EmployeeCode, Date)
    INCLUDE (CompanyId, CheckIn, EoS, TypeNom);

    PRINT 'Created index: IX_EmployeeCheckIns_Employee_Date';
END
GO

-- Índice para búsquedas por empresa y rango de fechas
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeCheckIns_Company_Date' AND object_id = OBJECT_ID('employee_check_ins'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeCheckIns_Company_Date
    ON employee_check_ins (CompanyId, Date)
    INCLUDE (EmployeeCode, CheckIn, EoS);

    PRINT 'Created index: IX_EmployeeCheckIns_Company_Date';
END
GO

-- Índice para filtrar por tipo de entrada/salida
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeCheckIns_EoS' AND object_id = OBJECT_ID('employee_check_ins'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeCheckIns_EoS
    ON employee_check_ins (EoS, Date)
    INCLUDE (EmployeeCode, CheckIn, CompanyId);

    PRINT 'Created index: IX_EmployeeCheckIns_EoS';
END
GO

-- ============================================
-- Tabla: Keys (claves de empleados)
-- Queries frecuentes: búsqueda por empresa, tipo de nómina, centro/supervisor
-- ============================================

-- Índice para búsquedas por empresa y tipo de nómina
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Keys_Company_TypeNom' AND object_id = OBJECT_ID('Keys'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Keys_Company_TypeNom
    ON Keys (Company, TypeNom)
    INCLUDE (Codigo, Center, Supervisor);

    PRINT 'Created index: IX_Keys_Company_TypeNom';
END
GO

-- Índice para búsquedas por centro (departamento)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Keys_Center' AND object_id = OBJECT_ID('Keys'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Keys_Center
    ON Keys (Center, Company)
    INCLUDE (Codigo, TypeNom);

    PRINT 'Created index: IX_Keys_Center';
END
GO

-- Índice para búsquedas por supervisor
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Keys_Supervisor' AND object_id = OBJECT_ID('Keys'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Keys_Supervisor
    ON Keys (Supervisor, Company)
    INCLUDE (Codigo, TypeNom);

    PRINT 'Created index: IX_Keys_Supervisor';
END
GO

-- ============================================
-- Tabla: Employees
-- Queries frecuentes: búsqueda por código, empresa, estado activo
-- ============================================

-- Índice para empleados activos por empresa
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Company_Active' AND object_id = OBJECT_ID('Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_Company_Active
    ON Employees (Company, Active)
    INCLUDE (Codigo, Name, LastName, MLastName, Salary);

    PRINT 'Created index: IX_Employees_Company_Active';
END
GO

-- Índice para búsqueda por código
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Codigo' AND object_id = OBJECT_ID('Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_Codigo
    ON Employees (Codigo, Company)
    INCLUDE (Name, LastName, MLastName, Active);

    PRINT 'Created index: IX_Employees_Codigo';
END
GO

-- ============================================
-- Tabla: Periods
-- Queries frecuentes: búsqueda por empresa, año, tipo de nómina
-- ============================================

-- Índice para búsquedas de períodos
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Periods_Company_Year_Type' AND object_id = OBJECT_ID('Periods'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Periods_Company_Year_Type
    ON Periods (Company, Year, TypePayroll)
    INCLUDE (NumPeriod, StartDate, ClosingDate, IsActive);

    PRINT 'Created index: IX_Periods_Company_Year_Type';
END
GO

-- ============================================
-- Tabla: IncidentCodes
-- Queries frecuentes: búsqueda por código, adicionales
-- ============================================

-- Índice para códigos de incidencia adicionales
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentCodes_IsAdditional' AND object_id = OBJECT_ID('IncidentCodes'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_IncidentCodes_IsAdditional
    ON IncidentCodes (IsAdditional)
    INCLUDE (Code, Label, WithOperation, RestrictedWithRoles);

    PRINT 'Created index: IX_IncidentCodes_IsAdditional';
END
GO

-- ============================================
-- Tabla: PeriodStatus
-- Queries frecuentes: verificar estado de período
-- ============================================

-- Índice para búsquedas de estado de período
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PeriodStatus_Type_Period_Company' AND object_id = OBJECT_ID('PeriodStatus'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PeriodStatus_Type_Period_Company
    ON PeriodStatus (TypePayroll, NumPeriod, CompanyId)
    INCLUDE (TenantId);

    PRINT 'Created index: IX_PeriodStatus_Type_Period_Company';
END
GO

-- ============================================
-- Tabla: Users (PrenominaApi)
-- Queries frecuentes: búsqueda por email, rol
-- ============================================

-- Índice para búsqueda por email
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_Email
    ON Users (Email)
    INCLUDE (Id, Name, Password, RoleId);

    PRINT 'Created index: IX_Users_Email';
END
GO

-- Índice para búsqueda por rol
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_RoleId' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_RoleId
    ON Users (RoleId)
    INCLUDE (Id, Email, Name);

    PRINT 'Created index: IX_Users_RoleId';
END
GO

-- ============================================
-- Tabla: UserCompanies
-- Queries frecuentes: búsqueda por usuario, empresa
-- ============================================

-- Índice para búsqueda por usuario
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserCompanies_UserId' AND object_id = OBJECT_ID('UserCompanies'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserCompanies_UserId
    ON UserCompanies (UserId)
    INCLUDE (CompanyId);

    PRINT 'Created index: IX_UserCompanies_UserId';
END
GO

-- ============================================
-- Actualizar estadísticas
-- ============================================

PRINT 'Updating statistics for all indexed tables...';

UPDATE STATISTICS assistance_incidents;
UPDATE STATISTICS employee_check_ins;
UPDATE STATISTICS Keys;
UPDATE STATISTICS Employees;
UPDATE STATISTICS Periods;
UPDATE STATISTICS IncidentCodes;
UPDATE STATISTICS PeriodStatus;
UPDATE STATISTICS Users;
UPDATE STATISTICS UserCompanies;

PRINT 'Statistics updated successfully.';
GO

-- ============================================
-- Verificar índices creados
-- ============================================

SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS KeyColumns,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS IncludedColumns
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name LIKE 'IX_%'
ORDER BY t.name, i.name;
GO

PRINT '============================================';
PRINT 'Performance indexes created successfully!';
PRINT '============================================';
