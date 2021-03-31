-- Тест на производительность.

-- В сравнении способы массового добавления и обновления:
-- 1) SqlBulkCopy с использованием тригера и MERGE внутри этого тригера (по PK)
-- 2) SqlBulkCopy без использованием тригера (по FK)
-- 3) MERGE с использованием хп и типа в качестве копируемой таблицы таблицы (добавление и обновление в бд по PK)
-- 4) MERGE с использованием хп и типа в качестве копируемой таблицы таблицы (добавление и обновление в бд по FK)

USE [Test]
GO

CREATE TABLE [dbo].[Points]
(
    [Id] INT PRIMARY KEY
)

DECLARE @temp INT = 1

WHILE (@temp <= 100000)
    BEGIN
        INSERT INTO [Points]
        VALUES (@temp)
        SET @temp += 1	
    END

CREATE TABLE [dbo].[RequestStatus]
(
    [Id] INT PRIMARY KEY NOT NULL,
    [Definition] NVARCHAR(25) NOT NULL
)

INSERT INTO [dbo].[RequestStatus]
VALUES (0, N'InProgress'),
       (1, N'Success')

CREATE TABLE [dbo].[Requests]
(
    [Id] BIGINT PRIMARY KEY NOT NULL,
    [StatusId] INT NOT NULL,
    CONSTRAINT [FK_Requests_RequestStatus] FOREIGN KEY ([StatusId]) REFERENCES [RequestStatus] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
)

CREATE TABLE [dbo].[PointsToRetrySqlBulkCopyPK]
(
    [PointId] INT PRIMARY KEY NOT NULL,
    [RequestId] BIGINT NOT NULL,
    CONSTRAINT [FK_PointsToRetrySqlBulkCopy_Requests] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE
)

CREATE TABLE [dbo].[PointsToRetryMergePK]
(
    [PointId] INT PRIMARY KEY NOT NULL,
    [RequestId] BIGINT NOT NULL,
    CONSTRAINT [FK_PointsToRetryMergePK_Requests] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE
)

CREATE TABLE [dbo].[PointsToRetrySqlBulkCopyFK]
(
    [Id] INT PRIMARY KEY IDENTITY (1,1) NOT NULL,
    [PointId] INT  NOT NULL,
    [RequestId] BIGINT NOT NULL,
    CONSTRAINT [FK_PointsToRetrySqlBulkCopyFK_Points] FOREIGN KEY ([PointId]) REFERENCES [Points] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT [FK_PointsToRetrySqlBulkCopyFK_Requests] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE
)

CREATE TABLE [dbo].[PointsToRetryMergeCopyFK]
(
    [Id] INT PRIMARY KEY IDENTITY (1,1) NOT NULL,
    [PointId] INT  NOT NULL,
    [RequestId] BIGINT NOT NULL,
    CONSTRAINT [FK_PointsToRetryMergeCopyFK_Points] FOREIGN KEY ([PointId]) REFERENCES [Points] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT [FK_PointsToRetryMergeCopyFK_Requests] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE
)

GO

CREATE PROCEDURE [dbo].[AddValuesRequests]
(
    @RequestId BIGINT,
    @StatusDefinition VARCHAR(25)
)
AS
BEGIN
    
    IF NOT EXISTS (SELECT * FROM [Requests] WHERE [Id] = @RequestId)
        BEGIN
            INSERT INTO [Requests]
            VALUES (@RequestId, (SELECT [Id] FROM [RequestStatus] WHERE [Definition] = @StatusDefinition))
        END
    ELSE
        BEGIN
            UPDATE [Requests]
            SET [StatusId] = (SELECT [Id] FROM [RequestStatus] WHERE [Definition] = @StatusDefinition)
            WHERE [Id] = @RequestId
        END

END

GO

CREATE TRIGGER InsertUpdatePointsToRetrySqlBulkCopyPK
ON [PointsToRetrySqlBulkCopyPK]
INSTEAD OF INSERT 
AS
BEGIN
    MERGE INTO [PointsToRetrySqlBulkCopyPK] AS target
    USING Inserted AS source
    ON (target.[PointId] = source.[PointId])
    WHEN MATCHED THEN 
        UPDATE 
        SET target.[RequestId] = source.[RequestId]
    WHEN NOT MATCHED BY target THEN
        INSERT ([PointId],[RequestId])
        VALUES (source.[PointId], source.[RequestId]);
END

GO

CREATE TYPE [dbo].[PointsToRetryMergePKTableType] AS Table 
(
    [PointId] INT NOT NULL,
    [RequestId] BIGINT NOT NULL
)

GO

CREATE PROCEDURE [dbo].[PointsToRetryMergePK_proc]
(
    @PointsToRetry [dbo].[PointsToRetryMergePKTableType] READONLY
)
AS
BEGIN
    MERGE INTO [PointsToRetryMergePK] AS p
    USING @PointsToRetry AS s
    ON p.[PointId] = s.[PointId]
    WHEN MATCHED THEN 
        UPDATE 
        SET p.[RequestId] = s.[RequestId]
    WHEN NOT MATCHED BY target THEN
        INSERT ([PointId],[RequestId])
        VALUES (s.[PointId], s.[RequestId]);
END

GO

CREATE PROCEDURE [dbo].[PointsToRetryMergeFK_proc]
(
    @PointsToRetry [dbo].[PointsToRetryMergePKTableType] READONLY
)
AS
BEGIN
    MERGE INTO [PointsToRetryMergeCopyFK] AS p
    USING @PointsToRetry AS s
    ON p.[PointId] = s.[PointId]
    WHEN MATCHED THEN 
        UPDATE 
        SET p.[RequestId] = s.[RequestId]
    WHEN NOT MATCHED BY target THEN
        INSERT ([PointId],[RequestId])
        VALUES (s.[PointId], s.[RequestId]);
END

GO

CREATE PROCEDURE [dbo].[GetPointsForRetry]
AS
BEGIN
    SET NOCOUNT ON

    SELECT [PointId]
    FROM [PointsToRetrySqlBulkCopyPK] AS p JOIN
         [Requests] AS r ON p.[RequestId] = r.[Id]
    WHERE r.StatusId = 0
END