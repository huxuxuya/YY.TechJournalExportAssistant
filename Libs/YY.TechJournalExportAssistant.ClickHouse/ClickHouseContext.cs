using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using YY.TechJournalExportAssistant.ClickHouse.Helpers;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalExportAssistant.Core.Helpers;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Helpers;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class ClickHouseContext : IDisposable
    {
        #region Private Members

        private ClickHouseConnection _connection;

        #endregion

        #region Constructors

        public ClickHouseContext(string connectionSettings)
        {
            ClickHouseHelpers.CreateDatabaseIfNotExist(connectionSettings);

            _connection = new ClickHouseConnection(connectionSettings);
            _connection.Open();

            var cmdDDL = _connection.CreateCommand();
            cmdDDL.CommandText = Resource.Query_CreateTable_EventDataStorage;
            cmdDDL.ExecuteNonQuery();
            cmdDDL.CommandText = Resource.Query_CreateTable_LogFilesStorage;
            cmdDDL.ExecuteNonQuery();
        }

        #endregion

        #region Public Methods

        #region RowsData

        public void SaveRowsData(TechJournalLogBase techJournalLog, List<EventData> eventData, string directoryName, string fileName)
        {
            using (ClickHouseBulkCopy bulkCopyInterface = new ClickHouseBulkCopy(_connection)
            {
                DestinationTableName = "EventData",
                BatchSize = 100000,
                MaxDegreeOfParallelism = 4
            })
            {
                var values = eventData.Select(i => new object[]
                {
                    techJournalLog.Name,
                    directoryName,
                    fileName,
                    i.Id,
                    i.Period,
                    i.Level,
                    i.Duration,
                    i.DurationSec,
                    i.EventName ?? string.Empty,
                    i.ServerContextName ?? string.Empty,
                    i.ProcessName ?? string.Empty,
                    i.SessionId ?? 0,
                    i.ApplicationName ?? string.Empty,
                    i.ClientId ?? 0,
                    i.ComputerName ?? string.Empty,
                    i.ConnectionId ?? 0,
                    i.UserName ?? string.Empty,
                    i.ApplicationId ?? 0,
                    i.Context ?? string.Empty,
                    i.ActionType.GetDescription() ?? string.Empty,
                    i.Database ?? string.Empty,
                    i.DatabaseCopy ?? string.Empty,
                    i.DBMS.GetPresentation() ?? string.Empty,
                    i.DatabasePID ?? string.Empty,
                    i.PlanSQLText ?? string.Empty,
                    i.Rows ?? 0,
                    i.RowsAffected ?? 0,
                    i.SQLText ?? string.Empty,
                    i.SQLQueryOnly ?? string.Empty,
                    i.SQLQueryParametersOnly ?? string.Empty,
                    i.SQLQueryHash ?? string.Empty,
                    i.SDBL?? string.Empty,
                    i.Description?? string.Empty,
                    i.Message?? string.Empty,
                    i.GetCustomFieldsAsJSON() ?? string.Empty
                }).AsEnumerable();

                var bulkResult = bulkCopyInterface.WriteToServerAsync(values);
                bulkResult.Wait();
            }
        }

        public void SaveRowsData(TechJournalLogBase techJournalLog, IDictionary<string, List<EventData>> eventData)
        {
            Dictionary<string, DateTime> maxPeriodByFiles = new Dictionary<string, DateTime>();
            List<object[]> rowsForInsert = new List<object[]>();
            foreach (var eventInfo in eventData)
            {
                FileInfo logFileInfo = new FileInfo(eventInfo.Key);
                foreach (var eventItem in eventInfo.Value)
                {
                    if (!maxPeriodByFiles.TryGetValue(logFileInfo.Name, out DateTime maxPeriodValue))
                    {
                        if (logFileInfo.Directory != null)
                            maxPeriodValue = GetRowsDataMaxPeriod(
                                techJournalLog,
                                logFileInfo.Directory.Name,
                                logFileInfo.Name
                            );
                        maxPeriodByFiles.Add(logFileInfo.Name, maxPeriodValue);
                    }
                    else
                    {
                        maxPeriodValue = DateTime.MinValue;
                    }
                    if (maxPeriodValue != DateTime.MinValue && eventItem.Period <= maxPeriodValue)
                        if (logFileInfo.Directory != null && RowDataExistOnDatabase(techJournalLog, eventItem, logFileInfo.Directory.Name, logFileInfo.Name))
                            continue;

                    if (logFileInfo.Directory != null)
                        rowsForInsert.Add(new object[]
                        {
                            techJournalLog.Name,
                            logFileInfo.Directory.Name,
                            eventInfo.Key,
                            eventItem.Id,
                            eventItem.Period,
                            eventItem.Level,
                            eventItem.Duration,
                            eventItem.DurationSec,
                            eventItem.EventName ?? string.Empty,
                            eventItem.ServerContextName ?? string.Empty,
                            eventItem.ProcessName ?? string.Empty,
                            eventItem.SessionId ?? 0,
                            eventItem.ApplicationName ?? string.Empty,
                            eventItem.ClientId ?? 0,
                            eventItem.ComputerName ?? string.Empty,
                            eventItem.ConnectionId ?? 0,
                            eventItem.UserName ?? string.Empty,
                            eventItem.ApplicationId ?? 0,
                            eventItem.Context ?? string.Empty,
                            eventItem.ActionType.GetDescription() ?? string.Empty,
                            eventItem.Database ?? string.Empty,
                            eventItem.DatabaseCopy ?? string.Empty,
                            eventItem.DBMS.GetPresentation() ?? string.Empty,
                            eventItem.DatabasePID ?? string.Empty,
                            eventItem.PlanSQLText ?? string.Empty,
                            eventItem.Rows ?? 0,
                            eventItem.RowsAffected ?? 0,
                            eventItem.SQLText ?? string.Empty,
                            eventItem.SQLQueryOnly ?? string.Empty,
                            eventItem.SQLQueryParametersOnly ?? string.Empty,
                            eventItem.SQLQueryHash ?? string.Empty,
                            eventItem.SDBL ?? string.Empty,
                            eventItem.Description ?? string.Empty,
                            eventItem.Message ?? string.Empty,
                            eventItem.GetCustomFieldsAsJSON() ?? string.Empty
                        });
                }
            }

            if (rowsForInsert.Count == 0)
                return;

            using (ClickHouseBulkCopy bulkCopyInterface = new ClickHouseBulkCopy(_connection)
            {
                DestinationTableName = "EventData",
                BatchSize = 100000,
                MaxDegreeOfParallelism = 4
            })
            {
                var bulkResult = bulkCopyInterface.WriteToServerAsync(rowsForInsert);
                bulkResult.Wait();
                rowsForInsert.Clear();
            }
        }

        public DateTime GetRowsDataMaxPeriod(TechJournalLogBase techJournalLog, string directoryName, string fileName)
        {
            DateTime output = DateTime.MinValue;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT
                        MAX(Period) AS MaxPeriod
                    FROM EventData AS RD
                    WHERE TechJournalLog = {techJournalLog:String}
                        AND DirectoryName = {directoryName:String}
                        AND FileName = {fileName:String}";
                command.AddParameterToCommand("techJournalLog", techJournalLog.Name);
                command.AddParameterToCommand("directoryName", directoryName);
                command.AddParameterToCommand("fileName", fileName);
                using (var cmdReader = command.ExecuteReader())
                    if (cmdReader.Read()) output = cmdReader.GetDateTime(0);
            }

            return output;
        }
        public bool RowDataExistOnDatabase(TechJournalLogBase techJournalLog, EventData eventData, string directoryName, string fileName)
        {
            bool output = false;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT
                        TechJournalLog,
                        Id,
                        Period
                    FROM EventData AS RD
                    WHERE TechJournalLog = {techJournalLog:String}
                        AND Id = {existId:Int64}
                        AND DirectoryName = {directoryName:String}
                        AND FileName = {fileName:String}
                        AND Period = {existPeriod:DateTime}";
                command.AddParameterToCommand("techJournalLog", DbType.AnsiString, techJournalLog.Name);
                command.AddParameterToCommand("existId", DbType.Int64, eventData.Id);
                command.AddParameterToCommand("existPeriod", DbType.DateTime, eventData.Period);
                command.AddParameterToCommand("directoryName", DbType.String, directoryName);
                command.AddParameterToCommand("fileName", DbType.String, fileName);
                using (var cmdReader = command.ExecuteReader())
                    if (cmdReader.Read()) output = true;
            }

            return output;
        }

        #endregion

        #region LogFiles

        public TechJournalPosition GetLogFilePosition(TechJournalLogBase techJournalLog, string directoryName)
        {
            var cmdGetLastLogFileInfo = _connection.CreateCommand();
            cmdGetLastLogFileInfo.CommandText =
                @"SELECT	                
	                LastEventNumber,
	                LastCurrentFileData,
	                LastStreamPosition
                FROM LogFiles AS LF
                WHERE TechJournalLog = {techJournalLog:String}
                    AND DirectoryName = {directoryName:String}
                    AND Id IN (
                        SELECT
                            MAX(Id) LastId
                        FROM LogFiles AS LF_LAST
                        WHERE LF_LAST.TechJournalLog = {techJournalLog:String}
                            AND LF_LAST.DirectoryName = {directoryName:String}
                    )";
            cmdGetLastLogFileInfo.AddParameterToCommand("techJournalLog", DbType.AnsiString, techJournalLog.Name);
            cmdGetLastLogFileInfo.AddParameterToCommand("directoryName", DbType.AnsiString, directoryName);

            TechJournalPosition output = null;
            using (var cmdReader = cmdGetLastLogFileInfo.ExecuteReader())
            {
                if (cmdReader.Read())
                {
                    string fileData = cmdReader.GetString(1)
                        .Replace("\\\\", "\\")
                        .FixNetworkPath();
                    output = new TechJournalPosition(
                        cmdReader.GetInt64(0),
                        fileData,
                        cmdReader.GetInt64(2));
                }
            }

            return output;
        }
        public void SaveLogPosition(TechJournalLogBase techJournalLog, TechJournalPosition position)
        {
            var dataFileInfo = new FileInfo(position.CurrentFileData);
            using (ClickHouseBulkCopy bulkCopyInterface = new ClickHouseBulkCopy(_connection)
            {
                DestinationTableName = "LogFiles",
                BatchSize = 1000000
            })
            {
                if (dataFileInfo.Directory != null)
                {
                    IEnumerable<object[]> values = new List<object[]>()
                    {
                        new object[]
                        {
                            techJournalLog.Name,
                            dataFileInfo.Directory.Name,
                            DateTime.Now.Ticks,
                            dataFileInfo.Name,
                            dataFileInfo.CreationTimeUtc,
                            dataFileInfo.LastWriteTimeUtc,
                            position.EventNumber,
                            position.CurrentFileData.Replace("\\", "\\\\"),
                            position.StreamPosition ?? 0
                        }
                    }.AsEnumerable();
                    var bulkResult = bulkCopyInterface.WriteToServerAsync(values);
                    bulkResult.Wait();
                }
            }
        }
        public IDictionary<string, TechJournalPosition> GetCurrentLogPositions(
            TechJournalLogBase techJournalLog,
            TechJournalSettings settings,
            KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem)
        {
            var cmdGetLastLogFileInfo = _connection.CreateCommand();
            cmdGetLastLogFileInfo.CommandText = Resource.Query_GetActualPositions;
            cmdGetLastLogFileInfo.AddParameterToCommand("techJournalLog", DbType.AnsiString, techJournalLog.Name);

            IDictionary<string, TechJournalPosition> output = new Dictionary<string, TechJournalPosition>();
            using (var cmdReader = cmdGetLastLogFileInfo.ExecuteReader())
            {
                while (cmdReader.Read())
                {
                    string fileName = cmdReader.GetString(7).Replace("\\\\", "\\");
                    string directoryName = cmdReader.GetString(1);
                    output.Add(directoryName, new TechJournalPosition(
                        cmdReader.GetInt64(6),
                        fileName,
                        cmdReader.GetInt64(8)
                        ));
                }
            }

            return output;
        }

        #endregion

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        #endregion
    }
}
