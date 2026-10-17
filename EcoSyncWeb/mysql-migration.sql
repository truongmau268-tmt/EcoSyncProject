CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;
ALTER DATABASE CHARACTER SET utf8mb4;
CREATE TABLE `AiChatLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserEmail` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Mode` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Reply` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Source` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_AiChatLogs` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AiSummaries` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProjectRegistrationId` int NOT NULL,Reload Window
    `VolunteerName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Strengths` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Weaknesses` longtext CHARACTER SET utf8mb4 NOT NULL,
    `MatchPercent` int NOT NULL,
    `Overall` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Source` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_AiSummaries` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Feedbacks` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProjectId` int NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Sentiment` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Rating` int NOT NULL,
    `AuthorName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `AuthorEmail` varchar(255) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Feedbacks` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `News` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ImageUrl` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_News` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProjectRegistrations` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProjectId` int NOT NULL,
    `FullName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Phone` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Email` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Note` longtext CHARACTER SET utf8mb4 NOT NULL,
    `RegisteredAt` datetime(6) NOT NULL,
    `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
    `IsCertified` tinyint(1) NOT NULL,
    `IsEvaluated` tinyint(1) NOT NULL,
    `CertificateCode` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ProjectRegistrations` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Projects` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ImageUrl` longtext CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StartTime` datetime(6) NOT NULL,
    `EndTime` datetime(6) NOT NULL,
    `Location` longtext CHARACTER SET utf8mb4 NOT NULL,
    `TargetVolunteers` int NULL,
    `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Projects` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Users` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FullName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Email` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Phone` longtext CHARACTER SET utf8mb4 NULL,
    `Role` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PasswordResetTokenHash` longtext CHARACTER SET utf8mb4 NULL,
    `PasswordResetTokenExpiresAt` datetime(6) NULL,
    `IsBlocked` tinyint(1) NOT NULL,
    `Skills` longtext CHARACTER SET utf8mb4 NULL,
    `Experience` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Feedbacks_ProjectId_AuthorEmail` ON `Feedbacks` (`ProjectId`, `AuthorEmail`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261015124730_InitialMySql', '8.0.32');
+