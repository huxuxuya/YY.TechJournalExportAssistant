using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using YY.TechJournalExportAssistant.ClickHouse;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalExportAssistant.Core.SharedBuffer;
using YY.TechJournalExportAssistant.Core.SharedBuffer.EventArgs;

namespace YY.TechJournalExportAssistantWithSharedBufferConsoleApp
{
    class Program
    {
        public static IConfiguration Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
        
        static async Task Main(string[] args)
        {
            TechJournalSettings settings = TechJournalSettings.Create(Configuration);
            
            TechJournalExport exportMaster = new TechJournalExport(settings, new TechJournalOnClickHouseTargetBuilder());
            exportMaster.OnErrorEvent += OnError;
            exportMaster.OnSendLogEvent += OnSend;
            await exportMaster.StartExport();

            Console.WriteLine("Good luck & bye!");
        }

        private static void OnError(
            TechJournalSettings.LogSourceSettings settings,
            OnErrorExportSharedBufferEventArgs args)
        {
            Console.WriteLine($"Ошибка при экспорте логов ({settings?.Name ?? "<>"}): {args.Exception}");
        }

        private static void OnSend(
            TechJournalSettings.LogSourceSettings settings,
            OnSendLogFromSharedBufferEventArgs args)
        {
            Console.WriteLine($"Отправка данных в хранилище ({settings?.Name ?? "<>"}):\n" +
                              $"Записей: {args._rows.Select(e => e.Value.Count).Sum()}\n" +
                              $"Актуальных позиций чтения: {args._positions.Count}");
        }
    }
}
