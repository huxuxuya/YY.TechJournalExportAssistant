using System.Collections.Generic;
using System.Linq;
using YY.TechJournalReaderAssistant;
using EventData = YY.TechJournalReaderAssistant.Models.EventData;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public class TechJournalOnBuffer : TechJournalOnTarget
    {
        #region Private Member Variables

        private const int _defaultPortion = 1000;
        private readonly int _portion;
        private TechJournalLogBase _techJournalLog;
        private TechJournalPosition _lastTechJournalFilePosition;
        private TechJournalSettings.LogSourceSettings _logSettings;
        private readonly LogBuffers _logBuffers;
        private IList<EventData> _rowsDataForPosition;


        #endregion

        #region Constructor

        public TechJournalOnBuffer() : this(null, _defaultPortion)
        {

        }
        public TechJournalOnBuffer(int portion) : this(null, portion)
        {
            _portion = portion;
        }
        public TechJournalOnBuffer(LogBuffers buffers, int portion)
        {
            _portion = portion;
            _rowsDataForPosition = new List<EventData>();
            _logBuffers = buffers;
        }

        #endregion

        #region Public Methods

        public void SetLogSettings(TechJournalSettings.LogSourceSettings logSettings)
        {
            _logSettings = logSettings;
        }
        public void SetLastPosition(TechJournalPosition position)
        {
            _lastTechJournalFilePosition = position;
        }
        public override TechJournalPosition GetLastPosition(string directoryName)
        {
            if (_lastTechJournalFilePosition != null)
                return _lastTechJournalFilePosition;

            TechJournalPosition position = _logBuffers.GetLastPosition(_logSettings, _techJournalLog, directoryName);

            _lastTechJournalFilePosition = position;
            return position;
        }
        public override void SaveLogPosition(TechJournalPosition position)
        {
            _lastTechJournalFilePosition = position;
            _logBuffers.SaveLogsAndPosition(_logSettings, position, _rowsDataForPosition);
            _rowsDataForPosition.Clear();
        }
        public override int GetPortionSize()
        {
            return _portion;
        }
        public override void Save(EventData rowData, string fileName)
        {
            Save(new List<EventData>()
            {
                rowData
            }, fileName);
        }

        public override void Save(IList<EventData> rowsData, string fileName)
        {
            _rowsDataForPosition.Clear();
            _rowsDataForPosition = rowsData.ToList();
        }
        public override void SetInformationSystem(TechJournalLogBase techJournalLog)
        {
            _techJournalLog = techJournalLog;
        }

        #endregion
    }
}
