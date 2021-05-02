using System.Collections.Generic;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public interface ITechJournalOnTargetBuilder
    {
        ITechJournalOnTarget CreateTarget(TechJournalSettings settings, KeyValuePair<LogBufferItemKey, LogBufferItem> logBufferItem);
        IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings, TechJournalLogBase techJournalLog);
        void SaveRowsData(TechJournalSettings settings, Dictionary<LogBufferItemKey, LogBufferItem> dataFromBuffer);
    }
}
