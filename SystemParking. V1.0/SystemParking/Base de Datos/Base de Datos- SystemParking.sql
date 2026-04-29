CREATE TABLE Usuarios(
    idUsuario int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre varchar(100),
    Usuario varchar(50) UNIQUE, 
    Contraseña varchar(60),
    EstaActivo bit DEFAULT 1 
)


CREATE TABLE Configuracion (
    idConfig int PRIMARY KEY DEFAULT 1,
    TarifaHora decimal(10,2) NOT NULL,
    CuposTotales int NOT NULL
)


INSERT INTO Configuracion (idConfig, TarifaHora, CuposTotales) VALUES (1, 15.00, 40);

 
CREATE TABLE Entradas (
    idEntrada int IDENTITY(1,1) PRIMARY KEY,
    Placa varchar(20) NOT NULL,
    Cajon int NOT NULL, 
    HoraIngreso datetime DEFAULT GETDATE(),
    idUsuarioEntrada int, 
    Estatus varchar(20) DEFAULT 'Activo', 
    FOREIGN KEY (idUsuarioEntrada) REFERENCES Usuarios(idUsuario)
)

CREATE TABLE Salidas (
    idSalida int IDENTITY(1,1) PRIMARY KEY,
    idEntrada int UNIQUE, 
    HoraSalida datetime DEFAULT GETDATE(),
    TiempoEstacionado varchar(50),
    TotalPagar decimal(10,2),
    idUsuarioSalida int,
    FOREIGN KEY (idEntrada) REFERENCES Entradas(idEntrada),
    FOREIGN KEY (idUsuarioSalida) REFERENCES Usuarios(idUsuario)
)