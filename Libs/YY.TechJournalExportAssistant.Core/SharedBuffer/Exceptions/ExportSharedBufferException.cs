using System;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer.Exceptions
{
    public class ExportSharedBufferException : Exception
    {
        public TechJournalSettings.LogSourceSettings Settings { get; }

        public ExportSharedBufferException(string message, Exception innerException, TechJournalSettings.LogSourceSettings settings)
            : base(message, innerException)
        {
            Settings = settings;
        }
    }
}
