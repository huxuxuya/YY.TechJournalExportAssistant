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

        private static void OnError(OnErrorExportSharedBufferEventArgs e)
        {
            Console.WriteLine($"Log name: {e?.Exception?.Settings?.Name ?? "Unknown"}\n" +
                              $"Error info: {e.Exception.ToString()}");
        }

        private static void OnSend(OnSendLogFromSharedBufferEventArgs args)
        {
            Console.WriteLine($"Отправка данных в хранилище:\n" +
                              $"Записей: {args.DataFromBuffer.Values.SelectMany(i => i.LogRows).Select(i => i.Value).Count()}\n" +
                              $"Актуальных позиций чтения: {args.DataFromBuffer.Values.Select(i => i.LogPosition).Count() }");
        }
    }
}
