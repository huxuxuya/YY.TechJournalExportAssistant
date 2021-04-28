using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YY.TechJournalExportAssistant.Core.SharedBuffer.EventArgs;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public class TechJournalExport
    {
        #region Private Members

        private readonly TechJournalSettings _settings;
        private readonly LogBuffers _logBuffers;
        private readonly ITechJournalOnTargetBuilder _techJournalTargetBuilder;

        #endregion

        #region Constructors

        public TechJournalExport(TechJournalSettings settings, ITechJournalOnTargetBuilder techJournalTargetBuilder)
        {
            _settings = settings;
            _logBuffers = new LogBuffers();
            _techJournalTargetBuilder = techJournalTargetBuilder;

            foreach (var logSourceSettings in _settings.LogSources)
            {
                _logBuffers.LogBuffer.TryAdd(logSourceSettings, new LogBufferItem());
            }
            foreach (var logBufferItem in _logBuffers.LogBuffer)
            {
                var logPositions = techJournalTargetBuilder.GetCurrentLogPositions(settings, logBufferItem);
                logBufferItem.Value.LogPositions = new ConcurrentDictionary<string, TechJournalPosition>(logPositions);
            }
        }

        #endregion

        #region Public Methods

        public async Task StartExport()
        {
            await StartExport(CancellationToken.None);
        }

        public async Task StartExport(CancellationToken cancellationToken)
        {
            List<Task> exportJobs = new List<Task>();
            foreach (var logSource in _settings.LogSources)
            {
                Task logExportTask = Task.Run(() => LogExportJob(logSource, cancellationToken), cancellationToken);
                exportJobs.Add(logExportTask);
                await Task.Delay(1000, cancellationToken);
            }

            await Task.Delay(10000, cancellationToken);

            exportJobs.Add(Task.Run(() => SendLogFromBuffer(cancellationToken), cancellationToken));

            Task.WaitAll(exportJobs.ToArray(), cancellationToken);
        }

        #endregion

        #region Private Methods

        private async Task LogExportJob(
            TechJournalSettings.LogSourceSettings settings,
            CancellationToken cancellationToken)
        {
            string techJournalPath = settings.SourcePath;
            TimeZoneInfo timeZone = settings.TimeZone;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                bool bufferBlocked = (_logBuffers.TotalItemsCount >= _settings.Export.Buffer.MaxBufferSizeItemsCount);

                try
                {
                    if (!bufferBlocked)
                    {
                        TechJournalManager tjManager = new TechJournalManager(techJournalPath);
                        foreach (var tjDirectory in tjManager.Directories)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;
                            bufferBlocked = (_logBuffers.TotalItemsCount >= _settings.Export.Buffer.MaxBufferSizeItemsCount);
                            if (bufferBlocked)
                                break;
                            if (!tjDirectory.DirectoryData.Exists)
                                continue;

                            using (TechJournalExportMaster exporter = new TechJournalExportMaster())
                            {
                                exporter.SetTechJournalPath(tjDirectory.DirectoryData.FullName, timeZone);

                                TechJournalOnBuffer target =
                                    new TechJournalOnBuffer(_logBuffers, settings.Portion);
                                target.SetLogSettings(settings);
                                target.SetInformationSystem(new TechJournalLogBase()
                                {
                                    Name = settings.Name,
                                    Description = settings.Description
                                });
                                exporter.SetTarget(target);
                                exporter.OnErrorExportData += OnErrorExportDataToBuffer;

                                while (exporter.NewDataAvailable())
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                        break;

                                    exporter.SendData();

                                    if (cancellationToken.IsCancellationRequested)
                                        break;
                                    bufferBlocked = (_logBuffers.TotalItemsCount >= _settings.Export.Buffer.MaxBufferSizeItemsCount);
                                    if (bufferBlocked)
                                        break;
                                }
                            }
                        }
                    }

                    if (bufferBlocked)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    else if (_settings.WatchMode.Use)
                    {
                        await Task.Delay(_settings.WatchMode.Periodicity, cancellationToken);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    RaiseOnError(settings, new OnErrorExportSharedBufferEventArgs(e));
                    await Task.Delay(60000, cancellationToken);
                }
            }
        }

        private async Task SendLogFromBuffer(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                bool needExport = false;

                if (_logBuffers.TotalItemsCount >= _settings.Export.Buffer.MaxItemCountSize)
                {
                    needExport = true;
                }
                else
                {
                    DateTime bufferCreated = _logBuffers.Created;
                    var createTimeLeftMs = (DateTime.Now - bufferCreated).TotalMilliseconds;
                    if (bufferCreated != DateTime.MinValue && createTimeLeftMs >= _settings.Export.Buffer.MaxSaveDurationMs)
                    {
                        needExport = true;
                    }
                }

                if (needExport)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    TechJournalSettings.LogSourceSettings lastSettings = null;

                    try
                    {
                        foreach (var logBufferItem in _logBuffers.LogBuffer)
                        {
                            lastSettings = logBufferItem.Key;
                            if (cancellationToken.IsCancellationRequested)
                                break;
                            if (logBufferItem.Value.LogRows.Count == 0)
                                continue;

                            lock (logBufferItem.Key.LockObject)
                            {
                                var eventsByFile = logBufferItem.Value.LogRows
                                    .Select(b => new
                                    {
                                        FileName = b.Key.File.FullName,
                                        EventData = b.Value
                                    })
                                    .GroupBy(g => g.FileName)
                                    .ToDictionary(
                                        g => g.Key,
                                        g => g.Select(e => e.EventData).ToList());

                                OnSend(logBufferItem.Key, new OnSendLogFromSharedBufferEventArgs(
                                    logBufferItem.Key, eventsByFile, logBufferItem.Value.LogPositions));

                                ITechJournalOnTarget target = _techJournalTargetBuilder.CreateTarget(_settings, logBufferItem);
                                
                                target.Save(eventsByFile);
                                eventsByFile.Clear();
                                logBufferItem.Value.LogRows.Clear();

                                foreach (var logPosition in logBufferItem.Value.LogPositions)
                                {
                                    target.SaveLogPosition(logPosition.Value);
                                }

                                logBufferItem.Value.Created = DateTime.MinValue;
                                logBufferItem.Value.LastUpdate = DateTime.MinValue;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        RaiseOnError(lastSettings, new OnErrorExportSharedBufferEventArgs(e));
                        await Task.Delay(1000, cancellationToken);
                    }
                }

                await Task.Delay(100, cancellationToken);

                if (!_settings.WatchMode.Use
                    && _logBuffers.TotalItemsCount == 0)
                {
                    break;
                }
            }
        }

        #endregion

        #region Events

        public delegate void OnSendLogFromSharedBufferEventArgsHandler(TechJournalSettings.LogSourceSettings settings, OnSendLogFromSharedBufferEventArgs args);
        public delegate void OnErrorExportSharedBufferEventArgsHandler(TechJournalSettings.LogSourceSettings settings, OnErrorExportSharedBufferEventArgs args);
        public event OnSendLogFromSharedBufferEventArgsHandler OnSendLogEvent;
        public event OnErrorExportSharedBufferEventArgsHandler OnErrorEvent;

        protected void OnSend(
            TechJournalSettings.LogSourceSettings settings,
            OnSendLogFromSharedBufferEventArgs args)
        {
            OnSendLogEvent?.Invoke(settings, args);
        }
        protected void RaiseOnError(
            TechJournalSettings.LogSourceSettings settings,
            OnErrorExportSharedBufferEventArgs args)
        {
            OnErrorEvent?.Invoke(settings, args);
        }
        private void OnErrorExportDataToBuffer(OnErrorExportDataEventArgs e)
        {
            RaiseOnError(null, new OnErrorExportSharedBufferEventArgs(e.Exception));
        }

        #endregion
    }
}
