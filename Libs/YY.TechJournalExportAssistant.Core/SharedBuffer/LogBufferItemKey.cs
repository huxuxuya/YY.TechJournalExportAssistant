using System;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public class LogBufferItemKey
    {
        public TechJournalSettings.LogSourceSettings Settings { get; }
        public DateTime Period { get; }
        public string LogFile { get; }

        public LogBufferItemKey(
            TechJournalSettings.LogSourceSettings setting,
            DateTime period,
            string logFile)
        {
            Settings = setting;
            Period = period;
            LogFile = logFile;
        }
    }
}
