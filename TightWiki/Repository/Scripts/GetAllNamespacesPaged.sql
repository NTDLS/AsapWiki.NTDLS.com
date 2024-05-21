SELECT
	P.[Namespace],
	Count(0) as [CountOfPages],

	@PageSize as PaginationSize,
	(
		SELECT
			Count(DISTINCT P.[Namespace]) / (@PageSize + 0.0)
		FROM
			[Page] as P
	) as PaginationCount
FROM
	[Page] as P
GROUP BY
	[Namespace]
ORDER BY
	P.[Namespace]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
