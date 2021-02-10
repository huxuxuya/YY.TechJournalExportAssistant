using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using ClickHouse.Client.ADO;
using Xunit;
using YY.TechJournalExportAssistant.ClickHouse;
using YY.TechJournalExportAssistant.ClickHouse.Helpers;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistant.Tests
{
    public class TechJournalExportAssistantToClickHouseTests
    {
        private readonly string _configFilePath;
        private readonly string _logDataPath;

        public TechJournalExportAssistantToClickHouseTests()
        {
            _configFilePath = GetConfigFile();

            string unitTestDirectory = Directory.GetCurrentDirectory();

            string logArchive = Path.Combine(unitTestDirectory, "TestData", "TestData_ServerAndClusterLogs.zip");
            _logDataPath = Path.Combine(unitTestDirectory, "TestData", "TestData_ServerAndClusterLogs");
            if (Directory.Exists(_logDataPath)) Directory.Delete(_logDataPath, true);
            ZipFile.ExtractToDirectory(logArchive, _logDataPath);
        }

        [Fact]
        public void ExportDataTest()
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile(_configFilePath, optional: true, reloadOnChange: true)
                .Build();

            string connectionString = Configuration.GetConnectionString("TechJournalDatabase");

            IConfigurationSection techJournalSection = Configuration.GetSection("TechJournal");
            string techJournalPath = _logDataPath;
            int watchPeriodSeconds = techJournalSection.GetValue("WatchPeriod", 60);
            int watchPeriodSecondsMs = watchPeriodSeconds * 1000;
            bool useWatchMode = techJournalSection.GetValue("UseWatchMode", false);
            int portion = techJournalSection.GetValue("Portion", 1000);
            string timeZoneName = techJournalSection.GetValue("TimeZone", string.Empty);

            TimeZoneInfo timeZone;
            if (string.IsNullOrEmpty(timeZoneName))
                timeZone = TimeZoneInfo.Local;
            else
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);

            IConfigurationSection techJournalLogSection = Configuration.GetSection("TechJournalLog");
            string techJournalName = techJournalLogSection.GetValue("Name", string.Empty);
            string techJournalDescription = techJournalLogSection.GetValue("Description", string.Empty);

            ClickHouseHelpers.DropDatabaseIfExist(connectionString);

            int eventNumberReader = 0;
            TechJournalManager tjManagerReader = new TechJournalManager(_logDataPath);
            foreach (var tjDirectoryReader in tjManagerReader.Directories)
            {
                TechJournalReader tjReaderTest = TechJournalReader.CreateReader(tjDirectoryReader.DirectoryData.FullName);
                while (tjReaderTest.Read())
                    eventNumberReader += 1;
            }

            while (true)
            {
                TechJournalManager tjManager = new TechJournalManager(techJournalPath);
                foreach (var tjDirectory in tjManager.Directories)
                {
                    if (!tjDirectory.DirectoryData.Exists)
                        continue;

                    using (TechJournalExportMaster exporter = new TechJournalExportMaster())
                    {
                        exporter.SetTechJournalPath(tjDirectory.DirectoryData.FullName, timeZone);

                        TechJournalOnClickHouse target = new TechJournalOnClickHouse(connectionString, portion);
                        target.SetInformationSystem(new TechJournalLogBase()
                        {
                            Name = techJournalName,
                            DirectoryName = tjDirectory.DirectoryData.Name,
                            Description = techJournalDescription
                        });
                        exporter.SetTarget(target);

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

            int eventNumberFromClickHouse = 0;
            using (ClickHouseConnection cn = new ClickHouseConnection(connectionString))
            {
                using (ClickHouseCommand cmd = new ClickHouseCommand(cn))
                {
                    cmd.CommandText = 
                    @"SELECT
	                    COUNT(*)
                    FROM EventData ed";
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                        eventNumberFromClickHouse = Convert.ToInt32(reader.GetValue(0));
                }
            }

            Assert.Equal(eventNumberReader, eventNumberFromClickHouse);
        }

        private string GetConfigFile()
        {
            // TODO
            // Перенести формирование конфигурационного файла в скрипты CI

            string configFilePath = "appsettings.json";
            if (!File.Exists(configFilePath))
            {
                configFilePath = "ci-appsettings.json";
            }

            if (!File.Exists(configFilePath))
                throw new Exception("Config file not found.");

            return configFilePath;
        }
    }
}
