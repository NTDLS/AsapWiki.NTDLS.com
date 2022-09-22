IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllConfigurationEntry]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllConfigurationEntry] AS'
END
GO


ALTER PROCEDURE [dbo].[GetAllConfigurationEntry] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[ConfigurationGroupId] as [ConfigurationGroupId],
		[Name] as [Name],
		[Value] as [Value],
		[Description] as [Description]
	FROM
		[ConfigurationEntry]

END--PROCEDURE