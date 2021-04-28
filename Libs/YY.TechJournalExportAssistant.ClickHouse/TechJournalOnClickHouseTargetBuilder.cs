using System.Collections.Generic;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class TechJournalOnClickHouseTargetBuilder : ITechJournalOnTargetBuilder
    {
        public ITechJournalOnTarget CreateTarget(TechJournalSettings settings, KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem)
        {
            ITechJournalOnTarget target = new TechJournalOnClickHouse(settings.ConnectionString,
                logBufferItem.Key.Portion);
            target.SetInformationSystem(new TechJournalLogBase()
            {
                Name = logBufferItem.Key.Name,
                Description = logBufferItem.Key.Description
            });

            return target;
        }

        public IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings, KeyValuePair<TechJournalSettings.LogSourceSettings, LogBufferItem> logBufferItem)
        {
            ITechJournalOnTarget target = CreateTarget(settings, logBufferItem);
            return target.GetCurrentLogPositions(settings, logBufferItem);
        }
    }
}
