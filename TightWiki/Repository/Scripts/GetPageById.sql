SELECT
	P.Id,
	P.[Name],
	PR.[Description],
	PR.Body,
	PR.Revision,
	P.Revision as LatestRevision,
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	PR.ModifiedByUserId,
	PR.ModifiedDate,
	MBU.AccountName as ModifiedByUserName
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
INNER JOIN users_db.Profile as MBU
	ON MBU.UserId = P.ModifiedByUserId
WHERE
	P.Id = @PageId
