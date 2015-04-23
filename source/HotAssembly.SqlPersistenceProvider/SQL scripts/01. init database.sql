USE [master]
GO
DECLARE @databaseName nvarchar(max) = 'HotAssembly'

DECLARE 
	@datapath nvarchar(max) = CONVERT(nvarchar(MAX), SERVERPROPERTY('InstanceDefaultDataPath')),
	@logpath nvarchar(max) = CONVERT(nvarchar(MAX), SERVERPROPERTY('InstanceDefaultLogPath'));

DECLARE @stmt nvarchar(MAX) = N'CREATE DATABASE [' + @databaseName + N']
ON PRIMARY 
( NAME = N''' + @databaseName + N''', FILENAME = ''' + @datapath + @databaseName + N'''), 
FILEGROUP [FileStream] CONTAINS FILESTREAM DEFAULT (NAME = N''' + @databaseName + N'.Filestream'', FILENAME = N''' + @datapath + @databaseName + N'.Filestream'')
LOG ON ( NAME = N''' + @databaseName + N'.Log'', FILENAME = N''' + @logpath + @databaseName + N'.Log.ldf'');
ALTER DATABASE [' + @databaseName + N'] SET  ENABLE_BROKER; 
ALTER DATABASE [' + @databaseName + N'] SET RECOVERY FULL;';

EXECUTE [sys].[sp_executesql] @stmt = @stmt;
GO
USE [HotAssembly];
GO
IF (NOT EXISTS (SELECT * FROM sys.[schemas] WHERE [name] = 'common'))
	EXEC sp_executesql @stmt = N'CREATE SCHEMA [common]';
GO
IF (OBJECT_ID(N'[common].[Bundle]') IS NULL)
	CREATE TABLE [common].[Bundle](
		[BundleId] nvarchar(128) NOT NULL,
		[BundleGuid] [uniqueidentifier] ROWGUIDCOL NOT NULL,
		[Bundle] [varbinary](MAX) FILESTREAM  NOT NULL,
	 CONSTRAINT [PK_Bundle] PRIMARY KEY CLUSTERED 
	(
		[BundleId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] FILESTREAM_ON [FILESTREAM],
	CONSTRAINT UQ_BundleGuid UNIQUE (BundleGuid)
	) ON [PRIMARY] FILESTREAM_ON [FILESTREAM]
GO

IF (OBJECT_ID(N'[common].[GetBundleFileStreamData]') IS NOT NULL)
	DROP PROCEDURE [common].[GetBundleFileStreamData];
GO
CREATE PROCEDURE [common].[GetBundleFileStreamData]
	@BundleId nvarchar(128),
	@BundlePath nvarchar(max) = NULL OUTPUT,
	@TransactionContext varbinary(MAX) = NULL OUTPUT
AS
SET NOCOUNT ON;
SELECT	@BundlePath = [Bundle].PathName()
FROM  [common].[Bundle]
WHERE BundleId = @BundleId;

SET @TransactionContext = GET_FILESTREAM_TRANSACTION_CONTEXT();
GO
IF (OBJECT_ID(N'[common].[PrepareSaveBundleFileStreamData]') IS NOT NULL)
	DROP PROCEDURE [common].[PrepareSaveBundleFileStreamData];
GO
CREATE PROCEDURE [common].[PrepareSaveBundleFileStreamData]
	@BundleId nvarchar(128),
	@BundlePath nvarchar(max) = NULL OUTPUT,
	@TransactionContext varbinary(MAX) = NULL OUTPUT
AS
SET NOCOUNT ON;

DECLARE @out table (
	BundlePath nvarchar(max)
);

INSERT [common].[Bundle] (
			[BundleId],
			[BundleGuid],
			[Bundle]
		)
OUTPUT 
	[Inserted].[Bundle].PathName()
	INTO @out ([BundlePath])
VALUES	(@BundleId, NEWID(), 0x00);

SELECT TOP 1
	@BundlePath = [BundlePath],
	@TransactionContext = GET_FILESTREAM_TRANSACTION_CONTEXT()
FROM @out;
GO