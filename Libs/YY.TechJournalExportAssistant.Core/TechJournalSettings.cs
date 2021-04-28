using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace YY.TechJournalExportAssistant.Core
{
    public class TechJournalSettings
    {
        public static TechJournalSettings Create(string configFile)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile(configFile, optional: true, reloadOnChange: true)
                .Build();

            return Create(Configuration);
        }
        public static TechJournalSettings Create(IConfiguration configuration)
        {
            return new TechJournalSettings(configuration);
        }

        public string ConnectionString { get; }
        public WatchModeSettings WatchMode { get; }
        public ExportSettings Export { get; }
        public IReadOnlyList<LogSourceSettings> LogSources { get; }

        private TechJournalSettings(IConfiguration configuration)
        {
            ConnectionString = configuration.GetConnectionString("TechJournalDatabase");

            var exportParametersSection = configuration.GetSection("Export");
            var bufferSection = exportParametersSection.GetSection("Buffer");
            var maxItemCountSize = bufferSection.GetValue("MaxItemCountSize", 10000);
            var maxSaveDurationMs = bufferSection.GetValue("MaxSaveDurationMs", 60000);
            var maxBufferSizeItemsCount = bufferSection.GetValue("MaxBufferSizeItemsCount", 100000);
            Export = new ExportSettings(
                new ExportSettings.BufferSettings(maxItemCountSize, maxSaveDurationMs, maxBufferSizeItemsCount));

            var watchModeSection = configuration.GetSection("WatchMode");
            var useWatchMode = watchModeSection.GetValue("Use", false);
            var periodicityWatchMode = watchModeSection.GetValue("Periodicity", 60000);
            WatchMode = new WatchModeSettings(useWatchMode, periodicityWatchMode);

            var logSourcesSection = configuration.GetSection("LogSources");
            var logSources = logSourcesSection.GetChildren();
            List<LogSourceSettings> logSourceSettings = new List<LogSourceSettings>();
            foreach (var logSource in logSources)
            {
                var techJournalLogName = logSource.GetValue("Name", string.Empty);
                var techJournalLogDescription = logSource.GetValue("Description", string.Empty);
                string sourcePath = logSource.GetValue("SourcePath", string.Empty);
                int portion = logSource.GetValue("Portion", 10000);
                string timeZoneName = logSource.GetValue("TimeZone", string.Empty);
                TimeZoneInfo timeZone;
                if (string.IsNullOrEmpty(timeZoneName))
                    timeZone = TimeZoneInfo.Local;
                else
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);

                logSourceSettings.Add(new LogSourceSettings(
                    techJournalLogName,
                    techJournalLogDescription,
                    sourcePath,
                    portion,
                    timeZone));
            }
            LogSources = logSourceSettings;
        }

        public class WatchModeSettings
        {
            public bool Use { get; }
            public int Periodicity { get; }

            public WatchModeSettings(bool use, int periodicity)
            {
                Use = use;
                Periodicity = periodicity;
            }
        }

        public class LogSourceSettings
        {
            public string Name { get; }
            public string Description { get; }
            public string SourcePath { get; }
            public int Portion { get; }
            public TimeZoneInfo TimeZone { get; }
            public object LockObject { get; }

            public LogSourceSettings(string name, string description, string sourcePath, int portion, TimeZoneInfo timeZone)
            {
                Name = name;
                Description = description;
                Portion = portion;
                SourcePath = sourcePath;
                TimeZone = timeZone;
                LockObject = new object();
            }
        }

        public class ExportSettings
        {
            public BufferSettings Buffer { get; }

            public ExportSettings(BufferSettings bufferSettings)
            {
                Buffer = bufferSettings;
            }

            public class BufferSettings
            {
                public long MaxItemCountSize { get; }
                public long MaxSaveDurationMs { get; }
                public long MaxBufferSizeItemsCount { get; }

                public BufferSettings(long maxItemCountSize, long maxSaveDurationMs, long maxBufferSizeItemsCount)
                {
                    MaxItemCountSize = maxItemCountSize;
                    MaxSaveDurationMs = maxSaveDurationMs;
                    MaxBufferSizeItemsCount = maxBufferSizeItemsCount;
                }
            }
        }
    }
}
