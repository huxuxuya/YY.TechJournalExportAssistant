using System;
using System.Collections.Concurrent;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core.SharedBuffer
{
    /// <summary>
    /// Элемент буфера логов
    /// </summary>
    public class LogBufferItem
    {
        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Дата последнего обновления
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Количество записей лога в буфере
        /// </summary>
        public long ItemsCount => LogRows.Count;

        /// <summary>
        /// Актуальная позиция чтения файла лога
        /// </summary>
        public TechJournalPosition LogPosition { get; set; }

        /// <summary>
        /// Записи логов
        /// </summary>
        public ConcurrentDictionary<EventKey, EventData> LogRows { get; set; }
        
        public LogBufferItem()
        {
            Created = DateTime.Now;
            LastUpdate = DateTime.MinValue;
            LogRows = new ConcurrentDictionary<EventKey, EventData>();
        }
    }
}
