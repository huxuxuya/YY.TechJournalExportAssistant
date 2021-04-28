﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalReaderAssistant;
using EventData = YY.TechJournalReaderAssistant.Models.EventData;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class TechJournalOnClickHouse : TechJournalOnTarget
    {
        #region Private Member Variables

        private const int _defaultPortion = 1000;
        private readonly int _portion;
        private DateTime _maxPeriodRowData;
        private TechJournalLogBase _techJournalLog;
        private readonly string _connectionString;
        private TechJournalPosition _lastTechJournalFilePosition;

        #endregion

        #region Constructor

        public TechJournalOnClickHouse() : this(null, _defaultPortion)
        {

        }
        public TechJournalOnClickHouse(int portion) : this(null, portion)
        {
            _portion = portion;
        }
        public TechJournalOnClickHouse(string connectionString, int portion)
        {
            _portion = portion;
            _maxPeriodRowData = DateTime.MinValue;

            if (connectionString == null)
            {
                IConfiguration Configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();
                _connectionString = Configuration.GetConnectionString("TechJournalDatabase");
            }

            _connectionString = connectionString;
        }

        #endregion

        #region Public Methods

        public override TechJournalPosition GetLastPosition(string directoryName)
        {
            if (_lastTechJournalFilePosition != null)
                return _lastTechJournalFilePosition;

            TechJournalPosition position;
            using(var context = new ClickHouseContext(_connectionString))
                position = context.GetLogFilePosition(_techJournalLog, directoryName);
            
            _lastTechJournalFilePosition = position;
            return position;
        }
        public override void SaveLogPosition(TechJournalPosition position)
        {
            using (var context = new ClickHouseContext(_connectionString))
            {
                context.SaveLogPosition(_techJournalLog, position);
            }

            _lastTechJournalFilePosition = position;
        }
        public override int GetPortionSize()
        {
            return _portion;
        }
        public override void Save(EventData rowData, string fileName)
        {
            IList<EventData> rowsData = new List<EventData>
            {
                rowData
            };
            Save(rowsData, fileName);
        }

        public override void Save(IList<EventData> rowsData, string fileName)
        {
            if(rowsData.Count == 0)
                return;

            FileInfo fileInfo = new FileInfo(fileName);
            using (var context = new ClickHouseContext(_connectionString))
            {
                if (_maxPeriodRowData == DateTime.MinValue)
                {
                    if (fileInfo.Directory != null)
                        _maxPeriodRowData = context.GetRowsDataMaxPeriod(
                            _techJournalLog,
                            fileInfo.Directory.Name,
                            fileName
                        );
                }

                List<EventData> newEntities = new List<EventData>();
                foreach (var itemRow in rowsData)
                {
                    if (itemRow == null)
                        continue;
                    if (_maxPeriodRowData != DateTime.MinValue && itemRow.Period <= _maxPeriodRowData)
                        if (fileInfo.Directory != null 
                            && context.RowDataExistOnDatabase(_techJournalLog, itemRow, fileInfo.Directory.Name, fileInfo.Name))
                            continue;

                    newEntities.Add(itemRow);
                }

                if (fileInfo.Directory != null)
                    context.SaveRowsData(_techJournalLog, newEntities, fileInfo.Directory.Name, fileName);
            }
        }
        public override void SetInformationSystem(TechJournalLogBase techJournalLog)
        {
            _techJournalLog = techJournalLog;
        }

        public override void Save(IDictionary<string, List<EventData>> rowsData)
        {
            if (rowsData.Count == 0)
                return;

            using (var context = new ClickHouseContext(_connectionString))
            {
                context.SaveRowsData(_techJournalLog, rowsData);
            }
        }

        public override IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings, KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem)
        {
            IDictionary<string, TechJournalPosition> positions;

            using (var context = new ClickHouseContext(_connectionString))
            {
                positions = context.GetCurrentLogPositions(_techJournalLog, settings, logBufferItem);
            }

            return positions;
        }

        #endregion
    }
}
