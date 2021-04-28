using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public class LogBuffers
    {
        public readonly ConcurrentDictionary<TechJournalSettings.LogSourceSettings, LogBufferItem> LogBuffer;
        public readonly object LockObject;

        public LogBuffers()
        {
            LogBuffer = new ConcurrentDictionary<TechJournalSettings.LogSourceSettings, LogBufferItem>();
            LockObject = new object();
        }

        /// <summary>
        /// Общее количество записей логов в буфере
        /// </summary>
        public long TotalItemsCount
        {
            get
            {
                long totalItemsCount = 0;

                foreach (var logBufferItem in LogBuffer)
                {
                    //lock (logBufferItem.Key.LockObject)
                    //{
                        totalItemsCount += logBufferItem.Value.ItemsCount;
                    //}
                }

                return totalItemsCount;
            }
        }

        public DateTime Created
        {
            get
            {
                DateTime bufferCreated = DateTime.MinValue;

                foreach (var logBufferItem in LogBuffer)
                {
                    //lock (logBufferItem.Key.LockObject)
                    //{
                        if (bufferCreated == DateTime.MinValue)
                        {
                            bufferCreated = logBufferItem.Value.Created;
                        } else if (bufferCreated < logBufferItem.Value.Created)
                        {
                            bufferCreated = logBufferItem.Value.Created;
                        }
                    //}
                }

                return bufferCreated;
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
                SaveLogPosition(logSettings, logFileInfo, position);
                SaveLogs(logSettings, rowsData, logFileInfo);
            }
        }

        private void SaveLogPosition(
            TechJournalSettings.LogSourceSettings logSettings, 
            FileInfo logFileInfo, 
            TechJournalPosition position)
        {
            LogBuffer.AddOrUpdate(
                logSettings,
                (settings) =>
                {
                    var newLogBufferItem = new LogBufferItem();

                    if (logFileInfo.Directory != null)
                        newLogBufferItem.LogPositions.TryAdd(
                            logFileInfo.Directory.Name,
                            position);

                    return newLogBufferItem;
                },
                (settings, logBufferItem) =>
                {
                    if (logFileInfo.Directory != null)
                        logBufferItem.LogPositions.AddOrUpdate(
                            logFileInfo.Directory.Name,
                            (fullDirectoryName) => position,
                            (fullDirectoryName, oldPosition) => position);

                    return logBufferItem;
                });
        }

        private void SaveLogs(
            TechJournalSettings.LogSourceSettings logSettings,
            IList<EventData> rowsData,
            FileInfo logFileInfo)
        {
            LogBuffer.AddOrUpdate(
                logSettings,
                (settings) =>
                {
                    var newBufferItem = new LogBufferItem();

                    newBufferItem.LastUpdate = DateTime.Now;
                    foreach (var rowData in rowsData)
                    {
                        newBufferItem.LogRows.TryAdd(new EventKey()
                        {
                            Id = Guid.NewGuid(),
                            File = logFileInfo
                        }, rowData);
                    }

                    return newBufferItem;
                },
                (settings, logBufferItem) =>
                {
                    DateTime operationDate = DateTime.Now;
                    if (logBufferItem.Created == DateTime.MinValue)
                        logBufferItem.Created = operationDate;
                    logBufferItem.LastUpdate = operationDate;
                    foreach (var rowData in rowsData)
                    {
                        logBufferItem.LogRows.TryAdd(new EventKey()
                        {
                            Id = Guid.NewGuid(),
                            File = logFileInfo
                        }, rowData);
                    }

                    return logBufferItem;
                });
        }

        public TechJournalPosition GetLastPosition(
            TechJournalSettings.LogSourceSettings logSettings, 
            TechJournalLogBase techJournalLog,
            string directoryName)
        {
            TechJournalPosition position = null;
            if (LogBuffer.TryGetValue(logSettings, out LogBufferItem bufferItem))
            {
                bufferItem.LogPositions.TryGetValue(directoryName, out position);
            }

            return position;
        }
    }
}
