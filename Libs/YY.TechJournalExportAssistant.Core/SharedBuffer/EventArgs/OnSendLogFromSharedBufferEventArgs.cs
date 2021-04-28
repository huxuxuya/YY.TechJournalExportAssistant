using System.Collections.Generic;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer.EventArgs
{
    public sealed class OnSendLogFromSharedBufferEventArgs : System.EventArgs
    {
        public TechJournalSettings.LogSourceSettings _settings { get; } 
        public IDictionary<string, List<EventData>> _rows { get; }
        public IReadOnlyDictionary<string, TechJournalPosition> _positions { get; }

        public OnSendLogFromSharedBufferEventArgs(
            TechJournalSettings.LogSourceSettings settings,
            IDictionary<string, List<EventData>> rows, 
            IReadOnlyDictionary<string, TechJournalPosition> positions)
        {
            _settings = settings;
            _rows = rows;
            _positions = positions;
        }
    }    
}
