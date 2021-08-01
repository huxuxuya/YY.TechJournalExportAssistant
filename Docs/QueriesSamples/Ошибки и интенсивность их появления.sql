SELECT
	TechJournalLog,
	EventName,
	Description,
	Context,
	Message,
	COUNT(*) AS "EventsCount",
	MIN(Period) AS "From",
	MAX(Period) AS "To"
FROM EventData
WHERE TechJournalLog = '<имя журнала>'
	AND EventName IN ('EXCP','EXCPCNTX')
GROUP BY TechJournalLog, EventName, Description, Context, Message
ORDER BY "EventsCount" DESC