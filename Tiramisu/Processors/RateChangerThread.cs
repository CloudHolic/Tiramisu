using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using OsuParser;
using OsuParser.Exceptions;
using OsuParser.Structures;
using OsuParser.Structures.Events;
using OsuParser.Structures.HitObjects;
using Tiramisu.Util;
using NLog;

// ReSharper disable AssignNullToNotNullAttribute
namespace Tiramisu.Processors
{
    public struct RateChangerThreadInput
    {
        public string Path { get; set; }
        public bool OszChecked { get; set; }
        public double Rate { get; set; }
        public string OutPutDir { get; set; }
    }

    public class RateChangerThread
    {
        private static class ThreadData
        {
            public static List<Beatmap> Map;
            public static string Directory;
            public static string OutputDir;
            public static List<string> OsuNameList;
            public static List<string> NewOsuNameList;
            public static List<string> Mp3NameList;
            public static double Rate;
            public static bool Nightcore;
        }

        private string _resultFile = string.Empty;

        private static volatile RateChangerThread _instance;
        private static readonly object Lock = new object();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool IsWorking { get; private set; }
        public bool IsErrorOccurred { get; private set; }

        public static RateChangerThread Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                            _instance = new RateChangerThread();
                    }
                }
                return _instance;
            }
        }

        private RateChangerThread()
        {
        }

        // Return after worker thread stops.
        public string StartWorker(RateChangerThreadInput info)
        {
            _resultFile = string.Empty;
            var thread = new Thread(Worker);

            thread.Start(info);
            thread.Join();
            
            return _resultFile;
        }

        private void Worker(object threadInfo)
        {
            string[] invalidString = { "\\", "/", ":", "*", "?", "\"", "<", ">", "|" };

            IsWorking = true;
            var info = (RateChangerThreadInput)threadInfo;

            try
            {
                ThreadData.Directory = info.Path;
                ThreadData.OutputDir = info.OutPutDir;
                ThreadData.OsuNameList = Directory.GetFiles(ThreadData.Directory, "*.osu").ToList();
                ThreadData.Map = new List<Beatmap>();
                foreach(var cur in ThreadData.OsuNameList)
                    ThreadData.Map.Add(Parser.LoadOsuFile(Path.Combine(ThreadData.Directory, cur)));
                ThreadData.Mp3NameList = Directory.GetFiles(ThreadData.Directory, "*.mp3").ToList();
                ThreadData.Rate = info.Rate;
                ThreadData.Nightcore = false;
                ThreadData.NewOsuNameList = new List<string>();
                for (var i = 0; i < ThreadData.OsuNameList.Count; i++)
                {
                    var tempName = ThreadData.Map[i].Meta.Artist + " - " + ThreadData.Map[i].Meta.Title + " (" +
                                   ThreadData.Map[i].Meta.Creator + ") [" + ThreadData.Map[i].Meta.Version + " x" + ThreadData.Rate +
                                   (ThreadData.Nightcore ? "_P" : "") + "].osu";
                    tempName = invalidString.Aggregate(tempName, (current, cur) => current.Replace(cur, ""));
                    ThreadData.NewOsuNameList.Add(tempName);
                }
            }
            catch
            {
                IsErrorOccurred = true;
                throw;
            }

            var mp3ThreadList = ThreadData.Mp3NameList.Select(cur => new Thread(() => Mp3Change(cur))).ToList();
            var patternThreadList = new List<Thread>();
            for (var i = 0; i < ThreadData.OsuNameList.Count; i++)
            {
                var temp = i;
                patternThreadList.Add(new Thread(() => PatternChange(ThreadData.Map[temp], ThreadData.NewOsuNameList[temp])));
            }

            foreach (var thread in mp3ThreadList)
                thread.Start();
            foreach (var thread in patternThreadList)
                thread.Start();

            foreach (var thread in mp3ThreadList)
                thread.Join();
            foreach (var thread in patternThreadList)
                thread.Join();

            var delFiles = new List<string>(ThreadData.NewOsuNameList);
            delFiles.AddRange(ThreadData.Mp3NameList.Select(cur =>
                Path.GetFileNameWithoutExtension(cur) + "_" + ThreadData.Rate + (ThreadData.Nightcore ? "_P" : "") +
                ".mp3"));

            if (IsErrorOccurred)
                foreach (var cur in delFiles)
                    File.Delete(Path.Combine(ThreadData.Directory, cur));
            else
            {
                string[] exts = { ".osu", ".mp3", ".osb" };

                if (info.OszChecked)
                {
                    var newDir = Path.GetFileName(ThreadData.Directory) + " x" + ThreadData.Rate + (ThreadData.Nightcore ? "_P" : "");
                    var zipFile = newDir + ".osz";
                    string newPath, zipPath;

                    foreach (var cur in invalidString)
                    {
                        newDir = newDir.Replace(cur, "");
                        zipFile = zipFile.Replace(cur, "");
                    }

                    if (ThreadData.OutputDir == ThreadData.Directory)
                    {
                        newPath = Path.Combine(Path.GetDirectoryName(ThreadData.Directory), newDir);
                        zipPath = Path.Combine(Path.GetDirectoryName(ThreadData.Directory), zipFile);
                    }
                    else
                    {
                        newPath = Path.Combine(ThreadData.OutputDir, newDir);
                        zipPath = Path.Combine(ThreadData.OutputDir, zipFile);
                    }

                    DirectoryUtil.CopyFolder(ThreadData.Directory, newPath, exts);
                    foreach (var cur in delFiles)
                        File.Move(Path.Combine(ThreadData.Directory, cur), Path.Combine(newPath, cur));

                    ZipUtil.CreateFromDirectory(newPath, zipPath, true);
                    Directory.Delete(newPath, true);
                    _resultFile = zipPath;
                }
                else if (ThreadData.OutputDir != ThreadData.Directory)
                {
                    DirectoryUtil.CopyFolder(ThreadData.Directory, ThreadData.OutputDir, exts);

                    foreach (var cur in delFiles)
                        File.Move(Path.Combine(ThreadData.Directory, cur), Path.Combine(ThreadData.OutputDir, cur));
                }
            }

            IsErrorOccurred = IsWorking = false;
        }

        private void Mp3Change(string mp3Name)
        {
            try
            {
                var curPath = Path.Combine(ThreadData.Directory, mp3Name);
                var newPath = Path.Combine(ThreadData.Directory, Path.GetFileNameWithoutExtension(mp3Name) +
                    "_" + ThreadData.Rate + (ThreadData.Nightcore ? "_P" : "") + ".mp3");

                var psInfo = new ProcessStartInfo("process.bat")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Ref"),
                    Arguments = "\"" + curPath + "\" \"" + newPath + (ThreadData.Nightcore ? "\" \"-rate=" : "\" \"-tempo=") +
                        Math.Round((ThreadData.Rate * 100) - 100) + "\""
                };

                var process = Process.Start(psInfo);
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                Log.Error(e, $"An error occurred in MP3Change({mp3Name})");
                IsErrorOccurred = true;
                throw;
            }
        }

        private void PatternChange(Beatmap map, string newFile)
        {
            try
            {
                var newOsu = Parser.CopyBeatmap(map);

                // General
                newOsu.Gen.AudioFilename = Path.GetFileNameWithoutExtension(newOsu.Gen.AudioFilename) + "_" +
                                           ThreadData.Rate + (ThreadData.Nightcore ? "_P" : "") + ".mp3";
                newOsu.Gen.PreviewTime = SafeRateChange(newOsu.Gen.PreviewTime);

                // Editor
                newOsu.Edit.Bookmarks = newOsu.Edit.Bookmarks.Select(SafeRateChange).ToList();

                // Metadata
                newOsu.Meta.Version = newOsu.Meta.Version + " x" + ThreadData.Rate + (ThreadData.Nightcore ? "_P" : "");
                newOsu.Meta.BeatmapId = -1;

                // Events
                newOsu.Events.Breaks = newOsu.Events.Breaks.Select(cur =>
                    Tuple.Create(SafeRateChange(cur.Item1), SafeRateChange(cur.Item2))).ToList();
                newOsu.Events.SbObjects = newOsu.Events.SbObjects.Select(cur => new SbObject(cur)
                {
                    FrameDelay = SafeRateChange(cur.FrameDelay),
                    ActionList = cur.ActionList.Select(action => new SbAction(action)
                    {
                        StartTime = SafeRateChange(action.StartTime),
                        EndTime = action.EndTime.HasValue ? (int?) SafeRateChange(action.EndTime.Value) : null
                    }).ToList()
                }).ToList();
                newOsu.Events.SampleSounds = newOsu.Events.SampleSounds
                    .Select(cur => new SbSound(cur) {Time = SafeRateChange(cur.Time)}).ToList();

                // TimingPoints
                newOsu.Timing = newOsu.Timing.Select(cur => new TimingPoint(cur)
                {
                    Offset = cur.Offset / ThreadData.Rate,
                    MsPerBeat = cur.MsPerBeat > 0 ? cur.MsPerBeat / ThreadData.Rate : cur.MsPerBeat
                }).ToList();

                // HitObjects
                newOsu.HitObjects = newOsu.HitObjects.Select<HitObject, HitObject>(cur =>
                {
                    switch (cur)
                    {
                        case Circle _:
                            return new Circle((Circle) cur) {Time = SafeRateChange(cur.Time)};
                        case Slider _:
                            return new Slider((Slider) cur) {Time = SafeRateChange(cur.Time)};
                        case Spinner _:
                            return new Spinner((Spinner) cur)
                            {
                                Time = SafeRateChange(cur.Time),
                                EndTime = SafeRateChange(((Spinner) cur).EndTime)
                            };
                        case LongNote _:
                            return new LongNote((LongNote) cur)
                            {
                                Time = SafeRateChange(cur.Time),
                                EndTime = SafeRateChange(((LongNote) cur).EndTime)
                            };
                        default:
                            throw new InvalidBeatmapException("Unknown hitobject type found.");
                    }
                }).ToList();

                Parser.SaveOsuFile(Path.Combine(ThreadData.Directory, newFile), newOsu);
            }
            catch (Exception e)
            {
                Log.Error(e, $"An error occurred in PatternChange({newFile})");
                IsErrorOccurred = true;
                throw;
            }
        }

        private static int SafeRateChange(int oper)
        {
            return (int) (oper / ThreadData.Rate);
        }
    }
}