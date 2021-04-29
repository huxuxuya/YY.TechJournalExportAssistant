using System.Collections.Generic;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core
{
    public interface ITechJournalOnTarget
    {
        TechJournalPosition GetLastPosition(string directoryName);
        void SaveLogPosition(TechJournalPosition position);
        void SaveLogPositions(List<TechJournalPosition> positions);
        int GetPortionSize();
        void SetInformationSystem(TechJournalLogBase techJournalLog);
        void Save(EventData eventData, string fileName);
        void Save(IList<EventData> eventData, string fileName);
        void Save(IDictionary<string, List<EventData>> rowsData);
        IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings,
            KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem);
    }
}
