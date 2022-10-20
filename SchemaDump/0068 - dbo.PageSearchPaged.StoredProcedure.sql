IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[PageSearchPaged]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[PageSearchPaged] AS'
END
GO

ALTER PROCEDURE [dbo].[PageSearchPaged]
(
	@PageNumber int = 1,
	@PageSize int = 0,
	@SearchTerms nvarchar(MAX) = null
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	DECLARE @PaginationSize int = @PageSize
	DECLARE @MinimumMatchScore Decimal(16, 2)
	DECLARE @TokenCount INT = (SELECT COUNT(0) FROM STRING_SPLIT(@SearchTerms ,','))
	
	DECLARE @PageTokens TABLE
	(
		PageId INT,
		[Match] Decimal(3, 2),
		[Weight] int,
		[Score] Decimal(16, 2)
	)

	SELECT
		@MinimumMatchScore = Cast(CE.[Value] as Decimal(16, 2))
	FROM
		[ConfigurationEntry] as CE
	INNER JOIN [ConfigurationGroup] as CG
		ON CG.Id = CE.ConfigurationGroupId
	WHERE
		CG.[Name] = 'Search'
		AND CE.[Name] = 'Minimum Match Score'

	IF(@PageSize = 0)
	BEGIN--IF
		SELECT
			@PaginationSize = Cast(CE.[Value] as Int)
		FROM
			[ConfigurationEntry] as CE
		INNER JOIN [ConfigurationGroup] as CG
			ON CG.Id = CE.ConfigurationGroupId
		WHERE
			CG.[Name] = 'Basic'
			AND CE.[Name] = 'Pagination Size'
	END--IF

	INSERT INTO @PageTokens
	(
		PageId,
		[Match],
		[Weight],
		[Score]
	)
	SELECT
		T.PageId,
		COUNT(0) / (@TokenCount + 0.0) as [Match],
		SUM(T.[Weight]) as [Weight],
		SUM(T.[Weight]) * (COUNT(0) / (@TokenCount + 0.0))
	FROM
		PageToken as T
	INNER JOIN STRING_SPLIT(@SearchTerms ,',') as ST
		ON ST.[value] = T.Token
	WHERE
		IsNull(ST.[value], '') <> ''
	GROUP BY
		T.PageId
	HAVING
		(COUNT(0) / (@TokenCount + 0.0)) >= 0.60

	SELECT
		P.Id,
		ST.[Score],
		ST.[Match],
		ST.[Weight],
		P.[Name],
		P.Navigation,
		P.[Description],
		P.Revision,
		P.CreatedByUserId,
		P.CreatedDate,
		P.ModifiedByUserId,
		P.ModifiedDate,
		Createduser.AccountName as CreatedByUserName,
		ModifiedUser.AccountName as ModifiedByUserName,
		@PaginationSize as PaginationSize,
		(
			SELECT
				CEILING(Count(0) / (@PaginationSize + 0.0))
			FROM
				[Page] as P
			INNER JOIN @PageTokens as ST
				ON ST.PageId = P.Id
		) as PaginationCount
	FROM
		[Page] as P
	INNER JOIN [User] as ModifiedUser
		ON ModifiedUser.Id = P.ModifiedByUserId
	INNER JOIN [User] as Createduser
		ON Createduser.Id = P.CreatedByUserId
	INNER JOIN @PageTokens as ST
		ON ST.PageId = P.Id
	ORDER BY
		ST.[Score] DESC,
		P.[Name],
		P.Id
	OFFSET ((@PageNumber - 1) * @PaginationSize) ROWS FETCH NEXT @PaginationSize ROWS ONLY

END--PROCEDURE