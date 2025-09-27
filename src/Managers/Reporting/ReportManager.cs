using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HarmonyLib;
using Lotus.Managers.Hotkeys;
using UnityEngine;
using VentLib.Options;
using VentLib.Options.IO;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.Reporting;

[LoadStatic]
public static class ReportManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ReportManager));

    private static readonly DirectoryInfo ReportingDirectory;
    private static readonly Dictionary<ReportTag, OrderedSet<IReportProducer>> ReportProducers = new();
    private static readonly LogLevel ReportLevel = LogLevel.High.Similar("REPORT", ConsoleColor.Green);
    private static Hotkey _hotkey;

    static ReportManager()
    {
        OptionManager reportingOptionManager = OptionManager.GetManager(file: "file_options.txt", managerFlags: OptionManagerFlags.IgnorePreset);
        var reportingDirectoryOption = new OptionBuilder().Name("Reporting Directory")
            .Description("The directory for storing various reports.\nDefault = reports")
            #if ANDROID
            .Value("lotus_reports")
            #else
            .Value("reports")
            #endif
            .IOSettings(settings => settings.UnknownValueAction = ADEAnswer.Allow)
            .BuildAndRegister(reportingOptionManager);

        #if ANDROID
        ReportingDirectory = new DirectoryInfo(Path.Combine(Application.persistentDataPath, reportingDirectoryOption.GetValue<string>()));
        #else
        ReportingDirectory = new DirectoryInfo(reportingDirectoryOption.GetValue<string>());
        #endif
        if (!ReportingDirectory.Exists) ReportingDirectory.Create();

        Enum.GetValues<ReportTag>().ForEach(t => ReportProducers.Add(t, new OrderedSet<IReportProducer>()));
        _hotkey = HotkeyManager.Bind(KeyCode.F3).Do(() => GenerateReport());
    }

    public static void AddProducer(IReportProducer reportProducer, params ReportTag[] tags)
    {
        if (tags.Length == 0) tags = Enum.GetValues<ReportTag>();
        tags.ForEach(t => ReportProducers[t].Add(reportProducer));
    }

    public static void RemoveProducer(IReportProducer reportProducer)
    {
        ReportProducers.Values.ForEach(s => s.Remove(reportProducer));
    }

    public static void GenerateReport(params ReportTag[] tags)
    {
        HashSet<IReportProducer> exhaustedReporters = new();
        Dictionary<string, List<ReportInfo>> reports = new();

        if (tags.Length == 0) tags = Enum.GetValues<ReportTag>();
        tags.SelectMany(t => ReportProducers[t]).ForEach(p =>
        {
            if (!exhaustedReporters.Add(p)) return;
            ReportInfo reportInfo = p.ProduceReport();
            reports.GetOrCompute(reportInfo.Header.FileName, () => new List<ReportInfo>()).Add(reportInfo);
        });

        if (reports.Count == 0) return;
        log.Log(ReportLevel, $"Generating Report | Tags: [{tags.Join()}]");

        FileStream reportStream = File.Open(Path.Join(ReportingDirectory.FullName, "latest.zip"), FileMode.OpenOrCreate);
        ZipArchive archive = new(reportStream, ZipArchiveMode.Create);

        reports.ForEach(kp =>
        {
            string fileName = kp.Key;
            List<ReportInfo> reportInfos = kp.Value;
            FileInfo fileInfo = new(fileName.TrimEnd('.'));
            if (fileInfo.Extension == "") fileName += ".txt";

            string content = "";
            string fileContent = "";
            reportInfos.ForEach(i =>
            {
                string infoContent = $"======[{i.Header.ReportName}]======\n" + i.Content + "\n";
                content += infoContent;
                if (i.Header.SaveToFile) fileContent += infoContent;
            });

            log.Log(ReportLevel, $"\nReport for {fileName}\n{content}");
            ZipArchiveEntry entry = archive.CreateEntry(fileName);
            StreamWriter writer = new(entry.Open());
            writer.Write(fileContent);
            writer.Close();
        });

        archive.Dispose();
    }
}