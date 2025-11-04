CREATE DATABASE IF NOT EXISTS Recebi_Database;
USE Recebi_Database;

CREATE TABLE Usuarios (
id_usuario INT AUTO_INCREMENT PRIMARY KEY,
nome VARCHAR(100) NOT NULL,
email VARCHAR(100) UNIQUE NOT NULL,
senha VARCHAR(255) NOT NULL,
telefone VARCHAR(15),
apart VARCHAR(10) DEFAULT NULL,
tipo_usuario ENUM('Morador', 'Porteiro', 'Sindico') NOT NULL,
status ENUM('Ativo','Inativo') NOT NULL DEFAULT 'Ativo'
);

CREATE TABLE Encomendas (
id_encomenda INT AUTO_INCREMENT PRIMARY KEY,
apartamento VARCHAR(10) NOT NULL,
id_usuario INT NOT NULL, -- morador que recebe
descricao VARCHAR(255) NOT NULL,
codigo_rastreio VARCHAR(50),
status ENUM('Pendente', 'Retirada') DEFAULT 'Pendente',
data_entrada DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
data_retirada DATETIME,
CONSTRAINT fk_encomendas_user FOREIGN KEY (id_usuario) REFERENCES Usuarios(id_usuario) ON DELETE CASCADE
);

CREATE TABLE Historico (
id_historico INT AUTO_INCREMENT PRIMARY KEY,
id_usuario INT,          
id_encomenda INT NULL,            
acao VARCHAR(255) NOT NULL,       
tipo VARCHAR(50) NOT NULL,       
data_hora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
detalhes TEXT NULL DEFAULT NULL,   
CONSTRAINT fk_historico_usuario FOREIGN KEY (id_usuario) REFERENCES Usuarios(id_usuario) ON DELETE SET NULL,
CONSTRAINT fk_historico_encomenda FOREIGN KEY (id_encomenda) REFERENCES Encomendas(id_encomenda) ON DELETE SET NULL
);