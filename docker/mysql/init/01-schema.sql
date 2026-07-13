-- MySQL dump 10.13  Distrib 8.4.9, for Linux (x86_64)
--
-- Host: localhost    Database: tipmolde_test
-- ------------------------------------------------------
-- Server version	8.4.9

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `FichasFopLinhas`
--

DROP TABLE IF EXISTS `FichasFopLinhas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `FichasFopLinhas` (
  `FichaFopLinha_id` int NOT NULL AUTO_INCREMENT,
  `FichaProducao_id` int NOT NULL,
  `Peca_id` int DEFAULT NULL,
  `Molde_id` int DEFAULT NULL,
  `Data` datetime(6) NOT NULL,
  `Ocorrencia` varchar(4000) NOT NULL,
  `Correcao` varchar(4000) DEFAULT NULL,
  `Responsavel_id` int NOT NULL,
  `CriadoEm` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`FichaFopLinha_id`),
  KEY `IX_FichasFopLinhas_FichaProducao_id` (`FichaProducao_id`),
  KEY `IX_FichasFopLinhas_Responsavel_id` (`Responsavel_id`),
  KEY `IX_FichasFopLinhas_Peca_id` (`Peca_id`),
  KEY `IX_FichasFopLinhas_Molde_id` (`Molde_id`),
  CONSTRAINT `FK_FichasFopLinhas_FichasProducao_FichaProducao_id` FOREIGN KEY (`FichaProducao_id`) REFERENCES `fichasproducao` (`FichaProducao_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_FichasFopLinhas_moldes_Molde_id` FOREIGN KEY (`Molde_id`) REFERENCES `moldes` (`Molde_id`) ON DELETE SET NULL,
  CONSTRAINT `FK_FichasFopLinhas_pecas_Peca_id` FOREIGN KEY (`Peca_id`) REFERENCES `pecas` (`Peca_id`) ON DELETE SET NULL,
  CONSTRAINT `FK_FichasFopLinhas_users_Responsavel_id` FOREIGN KEY (`Responsavel_id`) REFERENCES `users` (`User_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `FichasFraLinhas`
--

DROP TABLE IF EXISTS `FichasFraLinhas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `FichasFraLinhas` (
  `FichaFraLinha_id` int NOT NULL AUTO_INCREMENT,
  `FichaProducao_id` int NOT NULL,
  `Data` datetime(6) NOT NULL,
  `Alteracoes` varchar(4000) NOT NULL,
  `Verificado` tinyint(1) NOT NULL,
  `Responsavel_id` int NOT NULL,
  `CriadoEm` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`FichaFraLinha_id`),
  KEY `IX_FichasFraLinhas_FichaProducao_id` (`FichaProducao_id`),
  KEY `IX_FichasFraLinhas_Responsavel_id` (`Responsavel_id`),
  CONSTRAINT `FK_FichasFraLinhas_FichasProducao_FichaProducao_id` FOREIGN KEY (`FichaProducao_id`) REFERENCES `fichasproducao` (`FichaProducao_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_FichasFraLinhas_users_Responsavel_id` FOREIGN KEY (`Responsavel_id`) REFERENCES `users` (`User_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `FichasFrmLinhas`
--

DROP TABLE IF EXISTS `FichasFrmLinhas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `FichasFrmLinhas` (
  `FichaFrmLinha_id` int NOT NULL AUTO_INCREMENT,
  `FichaProducao_id` int NOT NULL,
  `Data` datetime(6) NOT NULL,
  `Defeito` varchar(2000) NOT NULL,
  `Pormenor` varchar(4000) DEFAULT NULL,
  `Verificado` tinyint(1) NOT NULL,
  `Responsavel_id` int NOT NULL,
  `CriadoEm` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`FichaFrmLinha_id`),
  KEY `IX_FichasFrmLinhas_FichaProducao_id` (`FichaProducao_id`),
  KEY `IX_FichasFrmLinhas_Responsavel_id` (`Responsavel_id`),
  CONSTRAINT `FK_FichasFrmLinhas_FichasProducao_FichaProducao_id` FOREIGN KEY (`FichaProducao_id`) REFERENCES `fichasproducao` (`FichaProducao_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_FichasFrmLinhas_users_Responsavel_id` FOREIGN KEY (`Responsavel_id`) REFERENCES `users` (`User_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `clientes`
--

DROP TABLE IF EXISTS `clientes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientes` (
  `Cliente_id` int NOT NULL AUTO_INCREMENT,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NIF` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Sigla` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Pais` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Email` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Telefone` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Cliente_id`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `encomendas`
--

DROP TABLE IF EXISTS `encomendas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `encomendas` (
  `Encomenda_id` int NOT NULL AUTO_INCREMENT,
  `NumeroEncomendaCliente` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NumeroProjetoCliente` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `NomeServicoCliente` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `NomeResponsavelCliente` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DataRegisto` datetime(6) NOT NULL,
  `Estado` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Cliente_id` int NOT NULL,
  PRIMARY KEY (`Encomenda_id`),
  KEY `FK_Encomendas_Clientes_Cliente_id` (`Cliente_id`),
  CONSTRAINT `FK_Encomendas_Clientes_Cliente_id` FOREIGN KEY (`Cliente_id`) REFERENCES `clientes` (`Cliente_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `encomendasmoldes`
--

DROP TABLE IF EXISTS `encomendasmoldes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `encomendasmoldes` (
  `EncomendaMolde_id` int NOT NULL AUTO_INCREMENT,
  `Quantidade` int NOT NULL,
  `Prioridade` int NOT NULL,
  `DataEntregaPrevista` datetime(6) NOT NULL,
  `Encomenda_id` int NOT NULL,
  `Molde_id` int NOT NULL,
  `Estado` varchar(30) NOT NULL DEFAULT 'PENDENTE',
  PRIMARY KEY (`EncomendaMolde_id`),
  KEY `FK_EncomendasMoldes_Encomendas_Encomenda_id` (`Encomenda_id`),
  KEY `FK_EncomendasMoldes_Moldes_Molde_id` (`Molde_id`),
  CONSTRAINT `FK_EncomendasMoldes_Encomendas_Encomenda_id` FOREIGN KEY (`Encomenda_id`) REFERENCES `encomendas` (`Encomenda_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_EncomendasMoldes_Moldes_Molde_id` FOREIGN KEY (`Molde_id`) REFERENCES `moldes` (`Molde_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=26 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `especificacoestecnicas`
--

DROP TABLE IF EXISTS `especificacoestecnicas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `especificacoestecnicas` (
  `Molde_id` int NOT NULL,
  `Largura` decimal(65,30) DEFAULT NULL,
  `Comprimento` decimal(65,30) DEFAULT NULL,
  `Altura` decimal(65,30) DEFAULT NULL,
  `PesoEstimado` decimal(65,30) DEFAULT NULL,
  `TipoInjecao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SistemaInjecao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Contracao` decimal(65,30) DEFAULT NULL,
  `AcabamentoPeca` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Cor` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MaterialMacho` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MaterialCavidade` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MaterialMovimentos` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MaterialInjecao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `LadoFixo` tinyint(1) NOT NULL,
  `LadoMovel` tinyint(1) NOT NULL,
  PRIMARY KEY (`Molde_id`),
  CONSTRAINT `FK_EspecificacoesTecnicas_Moldes_Molde_id` FOREIGN KEY (`Molde_id`) REFERENCES `moldes` (`Molde_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `eventosmaquinaindustrial`
--

DROP TABLE IF EXISTS `eventosmaquinaindustrial`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `eventosmaquinaindustrial` (
  `EventoMaquinaIndustrial_id` int NOT NULL AUTO_INCREMENT,
  `SessaoMaquinaIndustrial_id` int DEFAULT NULL,
  `Maquina_id` int NOT NULL,
  `IpMaquina` varchar(45) NOT NULL,
  `Protocolo` varchar(30) NOT NULL,
  `EstadoMaquina` varchar(40) NOT NULL,
  `OccurredAt` datetime(6) NOT NULL,
  `Programa` varchar(100) DEFAULT NULL,
  `ContadorPecas` int DEFAULT NULL,
  `CodigoOperador` varchar(100) DEFAULT NULL,
  `CodigoPeca` varchar(100) DEFAULT NULL,
  `CodigoMolde` varchar(100) DEFAULT NULL,
  `CamposEmFalta` varchar(255) DEFAULT NULL,
  `PayloadBruto` longtext,
  `EstadoResolucao` varchar(40) NOT NULL DEFAULT 'PENDENTE',
  `ResolvidoComoEstadoProducao` varchar(30) DEFAULT NULL,
  `FonteResolucao` varchar(60) DEFAULT NULL,
  `ResolvedAt` datetime(6) DEFAULT NULL,
  `RegistoProducao_id` int DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`EventoMaquinaIndustrial_id`),
  KEY `IX_eventosmaquinaindustrial_Sessao_id` (`SessaoMaquinaIndustrial_id`),
  KEY `IX_eventosmaquinaindustrial_Maquina_id` (`Maquina_id`),
  KEY `IX_eventosmaquinaindustrial_IpMaquina` (`IpMaquina`),
  KEY `IX_eventosmaquinaindustrial_EstadoMaquina` (`EstadoMaquina`),
  KEY `IX_eventosmaquinaindustrial_EstadoResolucao` (`EstadoResolucao`),
  KEY `IX_eventosmaquinaindustrial_OccurredAt` (`OccurredAt`),
  KEY `IX_eventosmaquinaindustrial_RegistoProducao_id` (`RegistoProducao_id`),
  CONSTRAINT `FK_eventosmaquinaindustrial_maquinas_Maquina_id` FOREIGN KEY (`Maquina_id`) REFERENCES `maquinas` (`Maquina_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_eventosmaquinaindustrial_registos_RegistoProducao_id` FOREIGN KEY (`RegistoProducao_id`) REFERENCES `registosproducao` (`Registo_Producao_id`) ON DELETE SET NULL,
  CONSTRAINT `FK_eventosmaquinaindustrial_sessoes_Sessao_id` FOREIGN KEY (`SessaoMaquinaIndustrial_id`) REFERENCES `sessoesmaquinaindustrial` (`SessaoMaquinaIndustrial_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fases_producao`
--

DROP TABLE IF EXISTS `fases_producao`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fases_producao` (
  `Fases_producao_id` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Fases_producao_id`)
) ENGINE=InnoDB AUTO_INCREMENT=97 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fichasdocumentos`
--

DROP TABLE IF EXISTS `fichasdocumentos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fichasdocumentos` (
  `FichaDocumento_id` int NOT NULL AUTO_INCREMENT,
  `FichaProducao_id` int NOT NULL,
  `CriadoPor_user_id` int NOT NULL,
  `Versao` int NOT NULL,
  `Origem` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NomeFicheiro` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TipoFicheiro` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CaminhoFicheiro` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `HashSha256` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Ativo` tinyint(1) NOT NULL,
  PRIMARY KEY (`FichaDocumento_id`),
  KEY `FK_FichasDocumentos_FichasProducao_FichaProducao_id` (`FichaProducao_id`),
  KEY `FK_FichasDocumentos_Users_CriadoPor_user_id` (`CriadoPor_user_id`),
  CONSTRAINT `FK_FichasDocumentos_FichasProducao_FichaProducao_id` FOREIGN KEY (`FichaProducao_id`) REFERENCES `fichasproducao` (`FichaProducao_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_FichasDocumentos_Users_CriadoPor_user_id` FOREIGN KEY (`CriadoPor_user_id`) REFERENCES `users` (`User_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fichasproducao`
--

DROP TABLE IF EXISTS `fichasproducao`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fichasproducao` (
  `FichaProducao_id` int NOT NULL AUTO_INCREMENT,
  `Tipo` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DataCriacao` datetime(6) NOT NULL,
  `EncomendaMolde_id` int NOT NULL,
  PRIMARY KEY (`FichaProducao_id`),
  KEY `FK_FichasProducao_EncomendasMoldes_EncomendaMolde_id` (`EncomendaMolde_id`),
  CONSTRAINT `FK_FichasProducao_EncomendasMoldes_EncomendaMolde_id` FOREIGN KEY (`EncomendaMolde_id`) REFERENCES `encomendasmoldes` (`EncomendaMolde_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=149 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fornecedores`
--

DROP TABLE IF EXISTS `fornecedores`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fornecedores` (
  `Fornecedor_id` int NOT NULL AUTO_INCREMENT,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NIF` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Telefone` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Morada` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Fornecedor_id`)
) ENGINE=InnoDB AUTO_INCREMENT=88 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `itenspedidomaterial`
--

DROP TABLE IF EXISTS `itenspedidomaterial`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `itenspedidomaterial` (
  `ItemPedidoMaterial_id` int NOT NULL AUTO_INCREMENT,
  `PedidoMaterial_id` int NOT NULL,
  `Peca_id` int NOT NULL,
  `Quantidade` int NOT NULL,
  PRIMARY KEY (`ItemPedidoMaterial_id`),
  KEY `FK_ItensPedidoMaterial_Pecas_Peca_id` (`Peca_id`),
  KEY `FK_ItensPedidoMaterial_PedidosMaterial_PedidoMaterial_id` (`PedidoMaterial_id`),
  CONSTRAINT `FK_ItensPedidoMaterial_Pecas_Peca_id` FOREIGN KEY (`Peca_id`) REFERENCES `pecas` (`Peca_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ItensPedidoMaterial_PedidosMaterial_PedidoMaterial_id` FOREIGN KEY (`PedidoMaterial_id`) REFERENCES `pedidosmaterial` (`PedidoMaterial_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=43 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `maquinas`
--

DROP TABLE IF EXISTS `maquinas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `maquinas` (
  `Maquina_id` int NOT NULL AUTO_INCREMENT,
  `Numero` int NOT NULL,
  `NomeModelo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IpAddress` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProtocoloComunicacao` varchar(30) DEFAULT NULL,
  `Estado` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FaseDedicada_id` int NOT NULL,
  PRIMARY KEY (`Maquina_id`),
  KEY `FK_Maquinas_Fases_Producao_FaseDedicada_id` (`FaseDedicada_id`),
  CONSTRAINT `FK_Maquinas_Fases_Producao_FaseDedicada_id` FOREIGN KEY (`FaseDedicada_id`) REFERENCES `fases_producao` (`Fases_producao_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=1235 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `moldes`
--

DROP TABLE IF EXISTS `moldes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `moldes` (
  `Molde_id` int NOT NULL AUTO_INCREMENT,
  `Numero` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NumeroMoldeCliente` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Numero_cavidades` int NOT NULL,
  `TipoPedido` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ImagemCapaPath` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Molde_id`)
) ENGINE=InnoDB AUTO_INCREMENT=110 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pecas`
--

DROP TABLE IF EXISTS `pecas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pecas` (
  `Peca_id` int NOT NULL AUTO_INCREMENT,
  `NumeroPeca` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Designacao` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Prioridade` int NOT NULL,
  `Quantidade` int NOT NULL,
  `Referencia` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MaterialDesignacao` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `TratamentoTermico` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Massa` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Observacao` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MaterialRecebido` tinyint(1) NOT NULL,
  `ProximaFase_id` int DEFAULT NULL,
  `Molde_id` int NOT NULL,
  PRIMARY KEY (`Peca_id`),
  KEY `IX_Pecas_ProximaFase_id` (`ProximaFase_id`),
  KEY `IX_Pecas_Molde_id` (`Molde_id`),
  CONSTRAINT `FK_Pecas_FasesProducao_ProximaFase_id` FOREIGN KEY (`ProximaFase_id`) REFERENCES `fases_producao` (`Fases_producao_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Pecas_Moldes_Molde_id` FOREIGN KEY (`Molde_id`) REFERENCES `moldes` (`Molde_id`)
) ENGINE=InnoDB AUTO_INCREMENT=400 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pedidosmaterial`
--

DROP TABLE IF EXISTS `pedidosmaterial`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pedidosmaterial` (
  `PedidoMaterial_id` int NOT NULL AUTO_INCREMENT,
  `DataPedido` datetime(6) NOT NULL,
  `DataRececao` datetime(6) DEFAULT NULL,
  `Estado` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Fornecedor_id` int NOT NULL,
  `UserConferente_id` int DEFAULT NULL,
  PRIMARY KEY (`PedidoMaterial_id`),
  KEY `IX_PedidosMaterial_Fornecedor_id` (`Fornecedor_id`),
  KEY `IX_PedidosMaterial_UserConferente_id` (`UserConferente_id`),
  CONSTRAINT `FK_PedidosMaterial_Fornecedores_Fornecedor_id` FOREIGN KEY (`Fornecedor_id`) REFERENCES `fornecedores` (`Fornecedor_id`),
  CONSTRAINT `FK_PedidosMaterial_Users_UserConferente_id` FOREIGN KEY (`UserConferente_id`) REFERENCES `users` (`User_id`)
) ENGINE=InnoDB AUTO_INCREMENT=97 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `projetos`
--

DROP TABLE IF EXISTS `projetos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `projetos` (
  `Projeto_id` int NOT NULL AUTO_INCREMENT,
  `NomeProjeto` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SoftwareUtilizado` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TipoProjeto` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CaminhoPastaServidor` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Molde_id` int NOT NULL,
  PRIMARY KEY (`Projeto_id`),
  KEY `FK_Projetos_Moldes_Molde_id` (`Molde_id`),
  CONSTRAINT `FK_Projetos_Moldes_Molde_id` FOREIGN KEY (`Molde_id`) REFERENCES `moldes` (`Molde_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `registosproducao`
--

DROP TABLE IF EXISTS `registosproducao`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `registosproducao` (
  `Registo_Producao_id` int NOT NULL AUTO_INCREMENT,
  `Estado_producao` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Data_hora` datetime(6) NOT NULL,
  `Fase_id` int NOT NULL,
  `Operador_id` int NOT NULL,
  `Peca_id` int NOT NULL,
  `Maquina_id` int DEFAULT NULL,
  PRIMARY KEY (`Registo_Producao_id`),
  KEY `FK_RegistosProducao_Maquinas_Maquina_id` (`Maquina_id`),
  KEY `IX_RegistosProducao_Fase_id` (`Fase_id`),
  KEY `IX_RegistosProducao_Operador_id` (`Operador_id`),
  KEY `IX_RegistosProducao_Peca_id` (`Peca_id`),
  CONSTRAINT `FK_RegistosProducao_Fases_Producao_Fase_id` FOREIGN KEY (`Fase_id`) REFERENCES `fases_producao` (`Fases_producao_id`),
  CONSTRAINT `FK_RegistosProducao_Maquinas_Maquina_id` FOREIGN KEY (`Maquina_id`) REFERENCES `maquinas` (`Maquina_id`),
  CONSTRAINT `FK_RegistosProducao_Pecas_Peca_id` FOREIGN KEY (`Peca_id`) REFERENCES `pecas` (`Peca_id`),
  CONSTRAINT `FK_RegistosProducao_Users_Operador_id` FOREIGN KEY (`Operador_id`) REFERENCES `users` (`User_id`)
) ENGINE=InnoDB AUTO_INCREMENT=52 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `registostempoprojeto`
--

DROP TABLE IF EXISTS `registostempoprojeto`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `registostempoprojeto` (
  `Registo_Tempo_Projeto_id` int NOT NULL AUTO_INCREMENT,
  `Estado_tempo` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Data_hora` datetime(6) NOT NULL,
  `Projeto_id` int NOT NULL,
  `Autor_id` int NOT NULL,
  PRIMARY KEY (`Registo_Tempo_Projeto_id`),
  KEY `FK_RegistosTempoProjeto_Projetos_Projeto_id` (`Projeto_id`),
  KEY `FK_RegistosTempoProjeto_Users_Autor_id` (`Autor_id`),
  CONSTRAINT `FK_RegistosTempoProjeto_Projetos_Projeto_id` FOREIGN KEY (`Projeto_id`) REFERENCES `projetos` (`Projeto_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_RegistosTempoProjeto_Users_Autor_id` FOREIGN KEY (`Autor_id`) REFERENCES `users` (`User_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=25 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `revisoes`
--

DROP TABLE IF EXISTS `revisoes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `revisoes` (
  `Revisao_id` int NOT NULL AUTO_INCREMENT,
  `NumRevisao` int NOT NULL,
  `DescricaoAlteracoes` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DataEnvioCliente` datetime(6) NOT NULL,
  `Aprovado` tinyint(1) DEFAULT NULL,
  `DataResposta` datetime(6) DEFAULT NULL,
  `FeedbackTexto` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FeedbackImagemPath` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Projeto_id` int NOT NULL,
  PRIMARY KEY (`Revisao_id`),
  KEY `FK_Revisoes_Projetos_Projeto_id` (`Projeto_id`),
  CONSTRAINT `FK_Revisoes_Projetos_Projeto_id` FOREIGN KEY (`Projeto_id`) REFERENCES `projetos` (`Projeto_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `revokedtokens`
--

DROP TABLE IF EXISTS `revokedtokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `revokedtokens` (
  `RevokedToken_id` int NOT NULL AUTO_INCREMENT,
  `Jti` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ExpiresAtUtc` datetime(6) NOT NULL,
  `RevokedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`RevokedToken_id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sessoesmaquinaindustrial`
--

DROP TABLE IF EXISTS `sessoesmaquinaindustrial`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sessoesmaquinaindustrial` (
  `SessaoMaquinaIndustrial_id` int NOT NULL AUTO_INCREMENT,
  `Maquina_id` int NOT NULL,
  `Operador_id` int NOT NULL,
  `Peca_id` int NOT NULL,
  `Fase_id` int NOT NULL,
  `RegistoProducaoInicio_id` int DEFAULT NULL,
  `RegistoProducaoConclusao_id` int DEFAULT NULL,
  `EstadoSessao` varchar(40) NOT NULL,
  `UltimoEstadoMaquina` varchar(40) DEFAULT NULL,
  `StartedAt` datetime(6) NOT NULL,
  `LastSeenAt` datetime(6) NOT NULL,
  `ClosedAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`SessaoMaquinaIndustrial_id`),
  KEY `IX_sessoesmaquinaindustrial_Maquina_id` (`Maquina_id`),
  KEY `IX_sessoesmaquinaindustrial_Operador_id` (`Operador_id`),
  KEY `IX_sessoesmaquinaindustrial_Peca_id` (`Peca_id`),
  KEY `IX_sessoesmaquinaindustrial_Fase_id` (`Fase_id`),
  KEY `IX_sessoesmaquinaindustrial_EstadoSessao` (`EstadoSessao`),
  KEY `IX_sessoesmaquinaindustrial_RegistoProducaoInicio_id` (`RegistoProducaoInicio_id`),
  KEY `IX_sessoesmaquinaindustrial_RegistoProducaoConclusao_id` (`RegistoProducaoConclusao_id`),
  CONSTRAINT `FK_sessoesmaquinaindustrial_fases_Fase_id` FOREIGN KEY (`Fase_id`) REFERENCES `fases_producao` (`Fases_producao_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_sessoesmaquinaindustrial_maquinas_Maquina_id` FOREIGN KEY (`Maquina_id`) REFERENCES `maquinas` (`Maquina_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_sessoesmaquinaindustrial_pecas_Peca_id` FOREIGN KEY (`Peca_id`) REFERENCES `pecas` (`Peca_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_sessoesmaquinaindustrial_registos_Conclusao` FOREIGN KEY (`RegistoProducaoConclusao_id`) REFERENCES `registosproducao` (`Registo_Producao_id`) ON DELETE SET NULL,
  CONSTRAINT `FK_sessoesmaquinaindustrial_registos_Inicio` FOREIGN KEY (`RegistoProducaoInicio_id`) REFERENCES `registosproducao` (`Registo_Producao_id`) ON DELETE SET NULL,
  CONSTRAINT `FK_sessoesmaquinaindustrial_users_Operador_id` FOREIGN KEY (`Operador_id`) REFERENCES `users` (`User_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `User_id` int NOT NULL AUTO_INCREMENT,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Password` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Role` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`User_id`)
) ENGINE=InnoDB AUTO_INCREMENT=396 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'tipmolde_test'
--

--
-- Dumping routines for database 'tipmolde_test'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-07-09 15:14:35
