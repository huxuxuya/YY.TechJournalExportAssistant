# Помощник экспорта технологического журнала

| Nuget-пакет | Актуальная версия | Описание |
| ----------- | ----------------- | -------- |
| YY.TechJournalExportAssistant.Core | [![NuGet version](https://badge.fury.io/nu/YY.TechJournalExportAssistant.Core.svg)](https://badge.fury.io/nu/YY.TechJournalExportAssistant.Core) | Базовый пакет |
| YY.TechJournalExportAssistant.ClickHouse | [![NuGet version](https://badge.fury.io/nu/YY.TechJournalExportAssistant.ClickHouse.svg)](https://badge.fury.io/nu/YY.TechJournalExportAssistant.ClickHouse) | Пакет для экспорта в базу ClickHouse |

Решение для экспорта данных технологического журнала платформы 1С:Предприятие 8.x в нестандартные хранилища данных. С помощью библиотеки **[YY.TechJournalReaderAssistant](https://github.com/YPermitin/YY.TechJournalExportAssistant)** реализовано чтение данных из файлов лога технологического журнала (*.log).

Последние новости об этой и других разработках, а также выходе других материалов, **[смотрите в Telegram-канале](https://t.me/DevQuietPlace)**.

### Состояние сборки
| Windows |  Linux |
|:-------:|:------:|
| [![Testing on Windows](https://github.com/YPermitin/YY.TechJournalExportAssistant/actions/workflows/dotnet-test-on-windows.yml/badge.svg)](https://github.com/YPermitin/YY.TechJournalExportAssistant/actions/workflows/dotnet-test-on-windows.yml) | [![Testing on Linux](https://github.com/YPermitin/YY.TechJournalExportAssistant/actions/workflows/dotnet-test-on-linux.yml/badge.svg)](https://github.com/YPermitin/YY.TechJournalExportAssistant/actions/workflows/dotnet-test-on-linux.yml)    |

### Code Climate

[![Maintainability](https://api.codeclimate.com/v1/badges/30500457be8c7e4f1562/maintainability)](https://codeclimate.com/github/YPermitin/YY.TechJournalExportAssistant/maintainability)

## Благодарности

Выражаю большую благодарность **[Алексею Бочкову](https://github.com/alekseybochkov)** как идейному вдохновителю. 

Именно его разработка была первой реализацией чтения и экспорта технологического журнала 1С - **[TJ_LOADER](https://github.com/alekseybochkov/tj_loader)**. Основную идею и некоторые примеры реализации взял именно из нее, но с полной переработкой архитектуры библиотеки.

## Состав репозитория

* Библиотеки
  * YY.TechJournalExportAssistant.Core - ядро библиотеки с основным функционалом чтения и передачи данных.
  * YY.TechJournalExportAssistant.ClickHouse - функционал для экспорта данных в базу ClickHouse.
* Примеры приложений
  * YY.TechJournalExportAssistantConsoleApp - пример приложения для экспорта данных в базу ClickHouse.
  * YY.TechJournalExportAssistantWithSharedBufferConsoleApp - пример приложения для экспорта в базу ClickHouse из множества источников логов с использованием общего буфера.

## Требования и совместимость

Работа библиотеки тестировалась со следующими версиями компонентов:

* Платформа 1С:Предприятие версии от 8.3.6 и выше.
* ClickHouse 20.9 и выше.

В большинстве случаев работоспособность подтверждается и на более старых версиях ПО, но меньше тестируется. Основная разработка ведется для Microsoft Windows, но некоторый функционал проверялся под *.nix.*

### Простой экспорт

Репозиторий содержим пример консольного приложения для экспорта данных в базу ClickHouse - **YY.YY.TechJournalExportAssistantConsoleApp**.

#### Конфигурация

Первое, с чего следует начать - это конфигурационный файл приложения "appsettings.json". Это JSON-файл со строкой подключения к базе данных, сведениями об технологическом журнале и параметрами его обработки. Располагается в корне каталога приложения.

```json
{
  "ConnectionStrings": {
    "TechJournalDatabase": "Host=127.0.0.1;Port=8123;Username=default;password=;Database=AmazingTechJournalDatabase;"
  },
  "TechJournalLog": {
    "Name": "AmazingTechJournalDatabase",
    "Description": "Технологический журнал. Очень разный."
  },
  "TechJournal": {
    "SourcePath": "C:\\TechJournalDirectory",
    "UseWatchMode": true,
    "WatchPeriod": 5,
    "Portion": 10000
  }
}
```

Секция **"ConnectionStrings"** содержит строку подключения **"TechJournalDatabase"** к базе данных для экспорта. База будет создана автоматически при первом запуске приложения. Также можно создать ее вручную, главное, чтобы структура была соответствующей. Имя строки подключения **"TechJournalDatabase"** - это значение по умолчанию. Контекст приложения будет использовать ее автоматически, если это не переопределено разработчиком явно.

Секция **"TechJournalLog"** содержит название для текущего журнала и ее описание, ведь вариантов конфигурации сбора технологического журнала может быть бесконечное количество. Эта настройка позволяет их разделять при хранении в одной базе.

Секция **"TechJournal"** содержит параметры обработки технологического журнала:

* **SourcePath** - путь к каталогу с файлами технологического журнала. Необходимо указывать каталог аналогично тому, как он был указан в файле настройки технологического журнала (т.е. в нем должны быть каталоги по процессам и т.д.).
* **UseWatchMode** - при значении false приложение завершит свою работу после загрузки всех данных. При значении true будет отслеживать появления новых данных пока приложение не будет явно закрыто.
* **WatchPeriod** - период в секундах, с которым приложение будет проверять наличие изменений. Используется, если параметр "UseWatchMode" установлен в true.
* **Portion** - количество записей, передаваемых в одной порции в хранилище.

Настройки "UseWatchMode" и "WatchPeriod" не относятся к библиотеке. Эти параметры добавлены лишь для примера консольного приложения и используется в нем же.

#### Пример использования

На следующем листинге показан пример использования библиотеки.

```csharp
class Program
{
    #region Private Static Member Variables

    private static long _totalRows;
    private static long _lastPortionRows;
    private static DateTime _beginPortionExport;
    private static DateTime _endPortionExport;

    #endregion

    #region Static Methods

    static void Main(string[] args)
    {
        IConfiguration Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        string connectionString = Configuration.GetConnectionString("TechJournalDatabase");

        IConfigurationSection eventLogSection = Configuration.GetSection("TechJournal");
        string techJournalPath = eventLogSection.GetValue("SourcePath", string.Empty);
        int watchPeriodSeconds = eventLogSection.GetValue("WatchPeriod", 60);
        int watchPeriodSecondsMs = watchPeriodSeconds * 1000;
        bool useWatchMode = eventLogSection.GetValue("UseWatchMode", false);
        int portion = eventLogSection.GetValue("Portion", 1000);

        IConfigurationSection techJournalSection = Configuration.GetSection("TechJournalLog");
        string techJournalName = techJournalSection.GetValue("Name", string.Empty);
        string techJournalDescription = techJournalSection.GetValue("Description", string.Empty);

        if (string.IsNullOrEmpty(techJournalPath))
        {
            Console.WriteLine("Не указан каталог с файлами данных технологического журнала.");
            Console.WriteLine("Для выхода нажмите любую клавишу...");
            Console.Read();
            return;
        }

        Console.WriteLine();
        Console.WriteLine();

        while (true)
        {
            TechJournalManager tjManager = new TechJournalManager(techJournalPath);
            foreach (var tjDirectory in tjManager.Directories)
            {
                if (!tjDirectory.DirectoryData.Exists)
                    continue;

                using (TechJournalExportMaster exporter = new TechJournalExportMaster())
                {
                    exporter.SetTechJournalPath(tjDirectory.DirectoryData.FullName);

                    TechJournalOnClickHouse target = new TechJournalOnClickHouse(connectionString, portion);
                    target.SetInformationSystem(new TechJournalLogBase()
                    {
                        Name = techJournalName,
                        Description = techJournalDescription
                    });
                    exporter.SetTarget(target);

                    exporter.BeforeExportData += BeforeExportData;
                    exporter.AfterExportData += AfterExportData;
                    exporter.OnErrorExportData += OnErrorExportData;

                    _beginPortionExport = DateTime.Now;
                    while (exporter.NewDataAvailable())
                        exporter.SendData();
                }
            }

            if (useWatchMode)
            {
                if (Console.KeyAvailable)
                    if (Console.ReadKey().KeyChar == 'q')
                        break;
                Thread.Sleep(watchPeriodSecondsMs);
            }
            else
            {
                break;
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Для выхода нажмите любую клавишу...");
        Console.Read();
    }

    #endregion
}
```

Так выглядят примеры обработчиков событий.

```csharp
    #region Events

    private static void BeforeExportData(BeforeExportDataEventArgs e)
    {
        _lastPortionRows = e.Rows.Count;
        _totalRows += e.Rows.Count;

        Console.SetCursorPosition(0, 0);
        Console.WriteLine("[{0}] Last read: {1}             ", DateTime.Now, e.Rows.Count);
    }

    private static void AfterExportData(AfterExportDataEventArgs e)
    {
        _endPortionExport = DateTime.Now;
        var duration = _endPortionExport - _beginPortionExport;

        Console.WriteLine("[{0}] Total read: {1}            ", DateTime.Now, _totalRows);
        Console.WriteLine("[{0}] {1} / {2} (sec.)           ", DateTime.Now, _lastPortionRows, duration.TotalSeconds);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Нажмите 'q' для завершения отслеживания изменений...");

        _beginPortionExport = DateTime.Now;
    }

    private static void OnErrorExportData(OnErrorExportDataEventArgs e)
    {
        Console.WriteLine(
            "Ошибка при экспорте данных." +
            "Критическая: {0}\n" +
            "\n" +
            "Содержимое события:\n" +
            "{1}" +
            "\n" +
            "Информация об ошибке:\n" +
            "\n" +
            "{2}",
            e.Critical, e.SourceData, e.Exception);
    }

    #endregion
```

С их помощью можно проанализировать какие данные будут выгружены и отказаться от выгрузки с помощью поля "Cancel" в параметре события "BeforeExportDataEventArgs" в событии "Перед экспортом данных". В событии "После экспорта данных" можно проанализировать выгруженные данные.

### Экспорт из множества источников логов

В некоторых задачах источников логов для экспорта может быть очень много. При использовании предыдущего подхода может возникнуть проблема, если запросов к хранилищу будет очень много. Это не только приводит к проблемам производительности, но и может привести к остановке работы хранилища логов. Например, ClickHouse не любит большое количество операций записи данных. В этом случае как-раз и поможет общий буфер для хранения данных до их передачи в хранилище. Именно для этих целей и был создан представленный ниже функционал.

#### Настройки для экспорта

В отличии от предыдущего примера, конфигурация при экспорте из множества источников логов будет выглядеть следующим образом.

```json
{
  "ConnectionStrings": {
    "TechJournalDatabase": "Host=127.0.0.1;Port=8123;Username=default;password=;Database=AmazingTechJournalWithBufferDatabase;"
  },
  "WatchMode": {
    "Use": true,
    "Periodicity": 10000
  },
  "Export": {
    "Buffer": {
      "MaxItemCountSize": 500000,
      "MaxSaveDurationMs": 60000,
      "MaxBufferSizeItemsCount":  1000000
    } 
  },
  "LogSources": [
    {
      "Name": "TechJournal1C 1",
      "Description": "Технологический журнал. Очень разный. 1",
      "SourcePath": "Q:\\TJ1",
      "Portion": 10000,
      "TimeZone": ""
    },
    {
      "Name": "TechJournal1C 2",
      "Description": "Технологический журнал. Очень разный. 2",
      "SourcePath": "Q:\\TJ2",
      "Portion": 10000,
      "TimeZone": ""
    },
    {
      "Name": "TechJournal1C 3",
      "Description": "Технологический журнал. Очень разный. 3",
      "SourcePath": "Q:\\TJ3",
      "Portion": 10000,
      "TimeZone": ""
    }
  ]
}
```

Секция **"ConnectionStrings"** содержит строку подключения **"TechJournalDatabase"** к базе данных для экспорта. База будет создана автоматически при первом запуске приложения. Также можно создать ее вручную, главное, чтобы структура была соответствующей. Имя строки подключения **"TechJournalDatabase"** - это значение по умолчанию. Контекст приложения будет использовать ее автоматически, если это не переопределено разработчиком явно.

Секция **"WatchMode"** содержит настройки режима "наблюдения", в котором библиотеки будут отслеживать появление новых записей в файлах логов. Параметр "Use" включает или отключает этот режим работы, а "Periodicity" указываем в миллисекундах периодичность, с которой выполняется проверка появления новых данных.

Секция **"Export.Buffer"** содержит настройки работы буфера:
    * **MaxBufferSizeItemsCount** - максимальное количество записей в буфере. По достижению этого размера запись в буфер останавливается.
    * **MaxItemCountSize** - количество записей, по достижению которого выполняется экспорт данных из буфера и очистка буфера от ранее выгруженных данных.
    * **MaxSaveDurationMs** - количество миллисекунд хранения записей в буфере. По истечению этого времени записи из буфера будут отправлены в хранилище в любом случае, независимо от количества записей в буфере.

Секция **"LogSources"** содержит список параметров обработки технологических журналов, для каждого из которых указываются параметры:

    * **Name** - имя источника логов.
    * **Description** - описание источника логов.
    * **SourcePath** - путь к каталогу с файлами технологического журнала. Необходимо указывать каталог аналогично тому, как он был указан в файле настройки технологического журнала (т.е. в нем должны быть каталоги по процессам и т.д.).
    * **Portion** - количество записей, передаваемых в одной порции в хранилище.
    * **TimeZone** - часовой пояс логов для корректной обработки дат.

Почти все настройки аналогич простому способу экспорта, кроме настроек буфера. От них зависит сколько памяти будет выделено для работы буфера и как часто будет выполняться запрос экспорта данных. Параметры нужно подбирать индивидуально, но можете для начала использовать стандартные настройки из примера и менять по обстоятельству.

#### Пример использования буфера

На следующем листинге инициализируем настройки экспорта из файла конфигурации (см. выше), настраиваем обработчики событий экспорта и запускаем сам экспорт.

```csharp
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

        private static void OnError(OnErrorExportSharedBufferEventArgs args)
        {
            Console.WriteLine($"Ошибка при экспорте логов: {args.Exception}");
        }

        private static void OnSend(OnSendLogFromSharedBufferEventArgs args)
        {
            Console.WriteLine($"Отправка данных в хранилище:\n" +
                              $"Записей: {args.DataFromBuffer.Values.SelectMany(i => i.LogRows).Select(i => i.Value).Count()}\n" +
                              $"Актуальных позиций чтения: {args.DataFromBuffer.Values.Select(i => i.LogPosition).Count() }");
        }
    }
}

```

В каком-то плане работа с экспортом через буфер выглядит даже проще, чем первый пример. Но за служебных классом "TechJournalExport" кроется многопоточная обработка чтения логов и отдельный поток экспорта данных в хранилище. Подойдите к настройкам буфера в этом случае разумно, чтобы использовать ресурсы сервера эффективно.

Также не забываем, что чтение файлов логов читается в один поток в рамках одной настройки логов ТЖ (одного каталога с логами). А вот несколько каталогов логов уже будут обрабатываться в отдельных потоках.

## Cценарии использования

Библиотека может быть использования для создания приложений для экспорта технологического журнала платформы 1С:Предприяние 8.ч в нестандартные хранилища, которые упрощают анализ данных и позволяют организовать эффективный мониторинг.

## TODO

Планы в части разработки:

* Добавить возможность экспорта данных в PostgreSQL
* Улучшение производительности и добавление bencmark'ов

## Лицензия

MIT - делайте все, что посчитаете нужным. Никакой гарантии и никаких ограничений по использованию.