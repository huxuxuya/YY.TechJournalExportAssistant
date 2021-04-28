using System;
using System.Collections;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class EventDataBulkInsertObject : IEnumerable
    {
        public string TechJournalLog { set; get; }
        public string DirectoryName { set; get; }
        public string FileName { set; get; }
        public long Id { set; get; }
        public DateTime Period { set; get; }
        public long Level { set; get; }
        public long Duration { set; get; }
        public long DurationSec { set; get; }
        public string EventName { set; get; }
        public string ServerContextName { set; get; }
        public string ProcessName { set; get; }
        public long? SessionId { set; get; }
        public string ApplicationName { set; get; }
        public long? ClientId { set; get; }
        public string ComputerName { set; get; }
        public long? ConnectionId { set; get; }
        public string UserName { set; get; }
        public long? ApplicationId { set; get; }
        public string Context { set; get; }
        public string ActionType { set; get; }
        public string Database { set; get; }
        public string DatabaseCopy { set; get; }
        public string DBMS { set; get; }
        public string DatabasePID { set; get; }
        public string PlanSQLText { set; get; }
        public long? Rows { set; get; }
        public long? RowsAffected { set; get; }
        public string SQLText { set; get; }
        public string SQLQueryOnly { set; get; }
        public string SQLQueryParametersOnly { set; get; }
        public string SQLQueryHash { set; get; }
        public string SDBL { set; get; }
        public string Description { set; get; }
        public string Message { set; get; }
        public string CustomEventData { set; get; }

        public IEnumerator GetEnumerator()
        {
            yield return TechJournalLog;
            yield return DirectoryName;
            yield return FileName;
            yield return Id;
            yield return Period;
            yield return Level;
            yield return Duration;
            yield return DurationSec;
            yield return EventName;
            yield return ServerContextName;
            yield return ProcessName;
            yield return SessionId;
            yield return ApplicationName;
            yield return ClientId;
            yield return ComputerName;
            yield return ConnectionId;
            yield return UserName;
            yield return ApplicationId;
            yield return Context;
            yield return ActionType;
            yield return Database;
            yield return DatabaseCopy;
            yield return DBMS;
            yield return DatabasePID;
            yield return PlanSQLText;
            yield return Rows;
            yield return RowsAffected;
            yield return SQLText;
            yield return SQLQueryOnly;
            yield return SQLQueryParametersOnly;
            yield return SQLQueryHash;
            yield return SDBL;
            yield return Description;
            yield return Message;
            yield return CustomEventData;
        }
    }
}
