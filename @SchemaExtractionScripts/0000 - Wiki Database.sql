SET NOCOUNT ON;

DECLARE @Text TABLE (Id Int NOT NULL IDENTITY(1,1), [Text] NVARCHAR(MAX))

INSERT INTO @Text SELECT 'CREATE DATABASE [TightWiki]'

SELECT [Text] FROM @Text ORDER BY Id
