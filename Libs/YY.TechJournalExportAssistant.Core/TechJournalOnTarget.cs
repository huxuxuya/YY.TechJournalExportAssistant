using System;
using System.Collections.Generic;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core
{
    public abstract class TechJournalOnTarget : ITechJournalOnTarget
    {
        #region Public Methods

        public virtual TechJournalPosition GetLastPosition(string directoryName)
        {
            throw new NotImplementedException();
        }

        public virtual int GetPortionSize()
        {
            throw new NotImplementedException();
        }

        public virtual void Save(EventData eventData, string fileName)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(IList<EventData> rowsData, string fileName)
        {
            throw new NotImplementedException();
        }

        public virtual void SaveLogPosition(TechJournalPosition position)
        {
            throw new NotImplementedException();
        }

        public virtual void SetInformationSystem(TechJournalLogBase techJournalLog)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(IDictionary<string, List<EventData>> rowsData)
        {
            throw new NotImplementedException();
        }

        public virtual IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings, KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
