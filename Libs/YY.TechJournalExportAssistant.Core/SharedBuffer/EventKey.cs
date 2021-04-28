using System;
using System.IO;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    public class EventKey
    {
        public Guid Id { get; set; }
        public FileInfo File { get; set; }
    }
}
