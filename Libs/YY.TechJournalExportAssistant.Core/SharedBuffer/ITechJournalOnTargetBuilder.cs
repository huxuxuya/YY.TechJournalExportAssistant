using System.Collections.Generic;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public interface ITechJournalOnTargetBuilder
    {
        ITechJournalOnTarget CreateTarget(TechJournalSettings settings, KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem);
        IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings, KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem);
    }
}
