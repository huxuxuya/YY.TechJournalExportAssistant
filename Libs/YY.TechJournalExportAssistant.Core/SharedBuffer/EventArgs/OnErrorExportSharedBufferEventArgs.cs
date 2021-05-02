using System;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer.EventArgs
{
    public sealed class OnErrorExportSharedBufferEventArgs : System.EventArgs
    {
        public OnErrorExportSharedBufferEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }    
}
