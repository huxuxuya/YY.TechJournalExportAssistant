using YY.TechJournalExportAssistant.Core.SharedBuffer.Exceptions;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer.EventArgs
{
    public sealed class OnErrorExportSharedBufferEventArgs : System.EventArgs
    {
        public OnErrorExportSharedBufferEventArgs(ExportSharedBufferException exception)
        {
            Exception = exception;
        }

        public ExportSharedBufferException Exception { get; }
    }    
}
