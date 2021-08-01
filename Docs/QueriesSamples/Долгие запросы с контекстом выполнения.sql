SELECT
    TechJournalLog AS "Log",
    UserName,
    MIN(Period) AS "From",
    MAX(Period) AS "To",
	SQLQuery AS "SQLText",
	TRIM(arrayStringConcat(Context, '         ')) AS Context,
	SUM(Duration) AS "DurationTotal",
	SUM(RowsReturn) AS "Rows",
	COUNT(*) AS "ExecutionCount",
	SQLQueryHash AS "QueryHash",
	MAX(SessionId) AS "SessionIdSample"
FROM
	(SELECT
		TechJournalLog,
		Period,
		SessionId,
		DirectoryName,
		FileName,	
		ServerContextName,
		ConnectionId,
		UserName,
		groupArray(Context) "Context",
		MAX(Duration) AS "Duration",
		MAX(SQLQuery) AS "SQLQuery",
		MAX(SQLQueryHash) AS "SQLQueryHash",
		MAX(RowsReturn) AS "RowsReturn",
		COUNT(Id) "Count"
	FROM (
		SELECT
			TechJournalLog,
			Period,
			SessionId,
			DirectoryName,
			FileName,
			UserName,
			ServerContextName,
			ConnectionId,
			DurationSec AS "Duration",
			Rows AS "RowsReturn",
			Context "Context",
			SQLQueryOnly AS "SQLQuery",
			SQLQueryHash AS "SQLQueryHash",
			Id "Id"
		FROM EventData ed 
		WHERE TechJournalLog = '<имя журнала>'
			AND (Context <> '' OR SQLQueryOnly <> '')
	) data
GROUP BY TechJournalLog,
	Period,
	DirectoryName,
	SessionId,
	UserName,
	FileName,
	ServerContextName,
	ConnectionId
) fulldata
WHERE SQLQuery <> ''
GROUP BY TechJournalLog,
    UserName,
    Context,
	SQLQuery,
	SQLQueryHash
ORDER BY "DurationTotal" DESC
LIMIT 100