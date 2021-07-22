using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YY.TechJournalExportAssistant.Core.SharedBuffer.EventArgs;
using YY.TechJournalExportAssistant.Core.SharedBuffer.Exceptions;
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
                _logBuffers.LogPositions.TryAdd(logSourceSettings,
                    new ConcurrentDictionary<string, TechJournalPosition>());

                var logPositions = techJournalTargetBuilder.GetCurrentLogPositions(settings, logSourceSettings.TechJournalLog);
                foreach (var logPosition in logPositions)
                {
                    FileInfo logFileInfo = new FileInfo(logPosition.Value.CurrentFileData);
                    _logBuffers.LogPositions.AddOrUpdate(logSourceSettings,
                        (settingsKey) =>
                        {
                            var newPositions = new ConcurrentDictionary<string, TechJournalPosition>();
                            if (logFileInfo.Directory != null)
                                newPositions.AddOrUpdate(logFileInfo.Directory.Name,
                                    (dirName) => logPosition.Value,
                                    (dirName, oldPosition) => logPosition.Value);
                            return newPositions;
                        },
                        (settingsKey, logBufferItemOld) =>
                        {
                            if (logFileInfo.Directory != null)
                                logBufferItemOld.AddOrUpdate(logFileInfo.Directory.Name,
                                    (dirName) => logPosition.Value,
                                    (dirName, oldPosition) => logPosition.Value);
                            return logBufferItemOld;
                        });
                }
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
                    RaiseOnError(new OnErrorExportSharedBufferEventArgs(
                        new ExportSharedBufferException("Log export job failed.", e, settings)));
                    await Task.Delay(60000, cancellationToken);
                }
            }
        }

        private async Task SendLogFromBuffer(CancellationToken cancellationToken)
        {
            DateTime lastExportDate = DateTime.Now;
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
                    var createTimeLeftMs = (DateTime.Now - lastExportDate).TotalMilliseconds;
                    if (lastExportDate != DateTime.MinValue && createTimeLeftMs >= _settings.Export.Buffer.MaxSaveDurationMs)
                    {
                        needExport = true;
                    }
                }

                if (needExport)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var itemsToUpload = _logBuffers.LogBuffer
                            .Select(i => i.Key)
                            .OrderBy(i => i.Period)
                            .ToList();

                        var dataToUpload = _logBuffers.LogBuffer
                            .Where(i => itemsToUpload.Contains(i.Key))
                            .ToDictionary(k => k.Key, v => v.Value);

                        OnSend(new OnSendLogFromSharedBufferEventArgs(dataToUpload));

                        _techJournalTargetBuilder.SaveRowsData(_settings, dataToUpload);

                        foreach (var itemToUpload in itemsToUpload)
                        {
                            _logBuffers.LogBuffer.TryRemove(itemToUpload, out _);
                        }
                        lastExportDate = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        RaiseOnError(new OnErrorExportSharedBufferEventArgs(
                            new ExportSharedBufferException("Send log from buffer failed.", e, null)));
                        await Task.Delay(60000, cancellationToken);
                    }
                }

                await Task.Delay(1000, cancellationToken);

                if (!_settings.WatchMode.Use
                    && _logBuffers.TotalItemsCount == 0)
                {
                    break;
                }
            }
        }

        #endregion

        #region Events

        public delegate void OnSendLogFromSharedBufferEventArgsHandler(OnSendLogFromSharedBufferEventArgs args);
        public delegate void OnErrorExportSharedBufferEventArgsHandler(OnErrorExportSharedBufferEventArgs args);
        public event OnSendLogFromSharedBufferEventArgsHandler OnSendLogEvent;
        public event OnErrorExportSharedBufferEventArgsHandler OnErrorEvent;

        protected void OnSend(OnSendLogFromSharedBufferEventArgs args)
        {
            OnSendLogEvent?.Invoke(args);
        }
        protected void RaiseOnError(OnErrorExportSharedBufferEventArgs args)
        {
            OnErrorEvent?.Invoke(args);
        }
        private void OnErrorExportDataToBuffer(OnErrorExportDataEventArgs e)
        {
            RaiseOnError(new OnErrorExportSharedBufferEventArgs(
                new ExportSharedBufferException("Export data to buffer failed.", e.Exception, null)));
        }

        #endregion
    }
}
