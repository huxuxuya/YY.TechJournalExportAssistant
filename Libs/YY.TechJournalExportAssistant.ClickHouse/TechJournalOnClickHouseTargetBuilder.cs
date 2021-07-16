using System.Collections.Generic;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class TechJournalOnClickHouseTargetBuilder : ITechJournalOnTargetBuilder
    {
        public ITechJournalOnTarget CreateTarget(TechJournalSettings settings, KeyValuePair<LogBufferItemKey, LogBufferItem> logBufferItem)
        {
            ITechJournalOnTarget target = new TechJournalOnClickHouse(settings.ConnectionString,
                logBufferItem.Key.Settings.Portion);
            target.SetInformationSystem(logBufferItem.Key.Settings.TechJournalLog);

            return target;
        }

        public IDictionary<string, TechJournalPosition> GetCurrentLogPositions(TechJournalSettings settings, TechJournalLogBase techJournalLog)
        {
            using (ClickHouseContext context = new ClickHouseContext(settings.ConnectionString))
            {
                return context.GetCurrentLogPositions(techJournalLog);
            }
        }

        public void SaveRowsData(
            TechJournalSettings settings,
            Dictionary<LogBufferItemKey, LogBufferItem> dataFromBuffer)
        {
            using (ClickHouseContext context = new ClickHouseContext(settings.ConnectionString))
            {
                context.SaveRowsData(dataFromBuffer);
            }
        }
    }
}
