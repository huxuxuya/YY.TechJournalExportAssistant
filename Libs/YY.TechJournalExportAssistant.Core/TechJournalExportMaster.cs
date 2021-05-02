using System;
using System.Collections.Generic;
using System.IO;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.EventArguments;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core
{
    public sealed class TechJournalExportMaster : ITechJournalExportMaster, IDisposable
    {
        #region Private Member Variables

        private string _techJournalLogPath;
        private ITechJournalOnTarget _target;
        private TechJournalReader _reader;
        private readonly List<EventData> _dataToSend;
        private int _portionSize;
        private TimeZoneInfo _logTimeZoneInfo;

        public delegate void BeforeExportDataHandler(BeforeExportDataEventArgs e);
        public event BeforeExportDataHandler BeforeExportData;
        public delegate void AfterExportDataHandler(AfterExportDataEventArgs e);
        public event AfterExportDataHandler AfterExportData;
        public delegate void OnErrorExportDataHandler(OnErrorExportDataEventArgs e);
        public event OnErrorExportDataHandler OnErrorExportData;

        #endregion

        #region Constructor

        public TechJournalExportMaster()
        {
            _dataToSend = new List<EventData>();
            _portionSize = 0;
            _logTimeZoneInfo = TimeZoneInfo.Local;
        }

        #endregion

        #region Public Methods

        public void SetEventLogPath(string eventLogPath, TimeZoneInfo timeZone)
        {
            _techJournalLogPath = eventLogPath;
            if (!string.IsNullOrEmpty(_techJournalLogPath))
            {
                _reader = TechJournalReader.CreateReader(_techJournalLogPath);
                _reader.SetTimeZone(timeZone);
            }
        }
        public void SetTechJournalPath(string techJournalLogPath, TimeZoneInfo timeZone)
        {
            _logTimeZoneInfo = timeZone;
            SetEventLogPath(techJournalLogPath, _logTimeZoneInfo);
        }
        public void SetTarget(ITechJournalOnTarget target)
        {
            _target = target;
            if (_target != null)
            {
                _portionSize = _target.GetPortionSize();
            }
        }
        public bool NewDataAvailable()
        {
            if (_reader == null)
                return false;
            if (_target == null)
                return false;
            if (_techJournalLogPath == null)
                return false;

            TechJournalPosition lastPosition = GetFixedCurrentPosition();

            _reader.AfterReadFile -= TechJournalReader_AfterReadFile;
            _reader.AfterReadEvent -= TechJournalReader_AfterReadEvent;
            _reader.OnErrorEvent -= TechJournalReader_OnErrorEvent;
            _reader.Reset();

            _reader.SetCurrentPosition(lastPosition);

            return _reader.Read();
        }
        public void SendData()
        {
            if (_reader == null || _target == null || _techJournalLogPath == null)
                return;

            TechJournalPosition lastPosition = GetFixedCurrentPosition();

            _reader.Reset();
            _reader.AfterReadFile += TechJournalReader_AfterReadFile;
            _reader.AfterReadEvent += TechJournalReader_AfterReadEvent;
            _reader.OnErrorEvent += TechJournalReader_OnErrorEvent;
            _reader.SetCurrentPosition(lastPosition);

            long totalReadEvents = 0;
            while (_reader.Read())
            {
                if (_reader.CurrentRow != null)
                    totalReadEvents += 1;
                if (totalReadEvents >= _portionSize)
                    break;
            }
            if (_dataToSend.Count > 0)
                SendDataCurrentPortion(_reader);
        }
        public TimeZoneInfo GetTimeZone()
        {
            return _logTimeZoneInfo;
        }
        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Reset();
                _reader.Dispose();
                _reader = null;
            }
        }

        #endregion

        #region Private Methods

        private TechJournalPosition GetFixedCurrentPosition()
        {
            DirectoryInfo logDirectoryInfo = new DirectoryInfo(_techJournalLogPath);
            string directoryName = logDirectoryInfo.Name;
            TechJournalPosition lastPosition = _target.GetLastPosition(directoryName);

            if (lastPosition != null)
            {
                FileInfo lastDataFileInfo = new FileInfo(lastPosition.CurrentFileData);

                if (lastPosition.CurrentFileData != null && _reader.CurrentFile != null)
                {
                    FileInfo currentDataFileInfo = new FileInfo(_reader.CurrentFile);

                    // В случае, если каталог последней позиции не совпадает 
                    // с текущим каталогом данных, то предыдущую позицию не учитываем
                    if (lastDataFileInfo.Directory != null && currentDataFileInfo.Directory != null)
                    {
                        if (lastDataFileInfo.Directory.FullName != currentDataFileInfo.Directory.FullName)
                            lastPosition = null;
                    }
                }

                // Если файла с данными уже нет, то предыдущую позицию не учитываем
                if (!lastDataFileInfo.Exists)
                {
                    lastPosition = null;
                }
            }

            return lastPosition;
        }
        private void SendDataCurrentPortion(TechJournalReader reader)
        {
            FileInfo currentFile = new FileInfo(reader.CurrentFile);

            RiseBeforeExportData(out var cancel);
            if (!cancel)
            {
                _target.Save(_dataToSend, currentFile.FullName);
                RiseAfterExportData(reader.GetCurrentPosition());
            }

            if (reader.CurrentFile != null)
            {
                _target.SaveLogPosition(reader.GetCurrentPosition());
            }
            _dataToSend.Clear();
        }

        private void RiseAfterExportData(TechJournalPosition currentPosition)
        {
            AfterExportDataHandler handlerAfterExportData = AfterExportData;
            handlerAfterExportData?.Invoke(new AfterExportDataEventArgs()
            {
                CurrentPosition = currentPosition
            });

        }
        private void RiseBeforeExportData(out bool cancel)
        {
            BeforeExportDataHandler handlerBeforeExportData = BeforeExportData;
            if (handlerBeforeExportData != null)
            {
                BeforeExportDataEventArgs beforeExportArgs = new BeforeExportDataEventArgs()
                {
                    Rows = _dataToSend
                };
                handlerBeforeExportData.Invoke(beforeExportArgs);
                cancel = beforeExportArgs.Cancel;
            }
            else
            {
                cancel = false;
            }
        }

        #endregion

        #region Events

        private void TechJournalReader_AfterReadEvent(TechJournalReader sender, AfterReadEventArgs args)
        {
            if (sender.CurrentRow == null)
                return;

            _dataToSend.Add(sender.CurrentRow);

            if (_dataToSend.Count >= _portionSize)
                SendDataCurrentPortion(sender);
        }
        private void TechJournalReader_AfterReadFile(TechJournalReader sender, AfterReadFileEventArgs args)
        {
            if (_dataToSend.Count >= 0)
                SendDataCurrentPortion(sender);

            TechJournalPosition position = sender.GetCurrentPosition();
            _target.SaveLogPosition(position);
        }
        private void TechJournalReader_OnErrorEvent(TechJournalReader sender, OnErrorEventArgs args)
        {
            OnErrorExportDataHandler handlerOnErrorExportData = OnErrorExportData;
            handlerOnErrorExportData?.Invoke(new OnErrorExportDataEventArgs()
            {
                Exception = args.Exception,
                SourceData = args.SourceData,
                Critical = args.Critical
            });
        }

        #endregion
    }
}
