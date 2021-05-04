using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public class LogBuffers
    {
        public readonly ConcurrentDictionary<LogBufferItemKey, LogBufferItem> LogBuffer;
        
        public ConcurrentDictionary<TechJournalSettings.LogSourceSettings, ConcurrentDictionary<string, TechJournalPosition>> LogPositions { get; }
        
        public LogBuffers()
        {
            LogBuffer = new ConcurrentDictionary<LogBufferItemKey, LogBufferItem>();
            LogPositions = new ConcurrentDictionary<TechJournalSettings.LogSourceSettings, ConcurrentDictionary<string, TechJournalPosition>>();
        }

        /// <summary>
        /// Общее количество записей логов в буфере
        /// </summary>
        public long TotalItemsCount
        {
            get
            {
                return LogBuffer
                    .Select(e => e.Value.ItemsCount)
                    .Sum();
            }
        }
        
        public void SaveLogsAndPosition(
            TechJournalSettings.LogSourceSettings logSettings,
            TechJournalPosition position,
            IList<EventData> rowsData
            )
        {
            lock (logSettings.LockObject)
            {
                var logFileInfo = new FileInfo(position.CurrentFileData);
                SaveLogs(logSettings, position, rowsData, logFileInfo);
            }
        }
        
        private void SaveLogs(
            TechJournalSettings.LogSourceSettings logSettings,
            TechJournalPosition position,
            IList<EventData> rowsData,
            FileInfo logFileInfo)
        {
            var newBufferItem = new LogBufferItem();
            newBufferItem.LogPosition = position;
            foreach (var rowData in rowsData)
            {
                newBufferItem.LogRows.TryAdd(new EventKey()
                {
                    Id = Guid.NewGuid(),
                    File = logFileInfo
                }, rowData);
            }

            LogBuffer.TryAdd(new LogBufferItemKey(logSettings, DateTime.Now, logFileInfo.FullName), 
                newBufferItem);

            LogPositions.AddOrUpdate(logSettings,
                (settings) =>
                {
                    var newPositions = new ConcurrentDictionary<string, TechJournalPosition>();
                    if (logFileInfo.Directory != null)
                        newPositions.AddOrUpdate(logFileInfo.Directory.Name,
                            (dirName) => position, 
                            (dirName, oldPosition) => position);
                    return newPositions;
                },
                (settings, logBufferItem) =>
                {
                    if (logFileInfo.Directory != null)
                        logBufferItem.AddOrUpdate(logFileInfo.Directory.Name,
                            (dirName) => position,
                            (dirName, oldPosition) => position);
                    return logBufferItem;
                });
        }
        
        public TechJournalPosition GetLastPosition(
            TechJournalSettings.LogSourceSettings logSettings, 
            TechJournalLogBase techJournalLog,
            string directoryName)
        {
            TechJournalPosition position = null;
            if (LogPositions.TryGetValue(logSettings, out ConcurrentDictionary<string, TechJournalPosition> settingPositions))
            {
                settingPositions.TryGetValue(directoryName, out position);
            }

            return position;
        }
    }
}
