﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FabricObserver.Observers.Utilities
{
    // Data from the /proc/status file
    internal struct ParsedStatus
    {
        internal int Pid;
        internal ulong VmHWM;
        internal ulong VmRSS;
        internal ulong RssAnon;
        internal ulong RsSFile;
        internal ulong RssShmem;
        internal ulong VmData;
        internal ulong VmSwap;
        internal ulong VmSize;
        internal ulong VmPeak;
    }

    /// <summary>
    /// This class contains method to read data from files under /proc directory on Linux.
    /// </summary>
    internal static class LinuxProcFS
    {
        internal const string RootPath = "/proc/";
        private const string StatuSFileName = "/status";

        /// <summary>
        /// Reads data from the /proc/meminfo file.
        /// </summary>
        internal static Dictionary<string, ulong> ReadMemInfo()
        {
            // Currently /proc/meminfo contains 51 rows on Ubuntu 18.
            Dictionary<string, ulong> result = new Dictionary<string, ulong>(capacity: 64);

            // Source code: https://git.kernel.org/pub/scm/linux/kernel/git/torvalds/linux.git/tree/fs/proc/meminfo.c
            using (StreamReader sr = new StreamReader("/proc/meminfo", encoding: Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    int colonIndex = line.IndexOf(':');

                    string key = line.Substring(0, colonIndex);

                    ulong value = LinuxProcFS.ReadUInt64(line, colonIndex + 1);
                    result.Add(key, value);
                }
            }

            return result;
        }

        /// <summary>
        /// Reads data from the /proc/uptime file.
        /// </summary>
        /// <returns>An Uptime/IdleTime tuple. The first value represents the total number of seconds the system has been up.
        /// The second value is the sum of how much time each core has spent idle, in seconds.</returns>
        internal static async Task<(float Uptime, float IdleTime)> ReadUptimeAsync()
        {
            // Doc: https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/6/html/deployment_guide/s2-proc-uptime
            // Source code: https://git.kernel.org/pub/scm/linux/kernel/git/torvalds/linux.git/tree/fs/proc/uptime.c
            string text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync("/proc/uptime"));

            int spaceIndex = text.IndexOf(' ');

            float uptime = float.Parse(text.AsSpan(0, spaceIndex));
            float idleTime = float.Parse(text.AsSpan(spaceIndex + 1));

            return (uptime, idleTime);
        }

        /// <summary>
        /// Parses /proc/{pid}/status file.
        /// </summary>
        internal static bool TryParseStatuSFile(int pid, out ParsedStatus result)
        {
            string statuSFilePath = RootPath + pid.ToString() + StatuSFileName;

            return TryParseStatuSFile(statuSFilePath, out result);
        }

        /// <summary>
        /// Parses /proc/{pid}/status file.
        /// </summary>
        internal static bool TryParseStatuSFile(string statuSFilePath, out ParsedStatus result)
        {
            result = default(ParsedStatus);

            if (!TryReadFile(statuSFilePath, out string[] fileLines))
            {
                return false;
            }

            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i];

                int idx = line.IndexOf(':');

                string title = line.Substring(0, idx);

                switch (title)
                {
                    case nameof(ParsedStatus.Pid):
                        result.Pid = (int)ReadUInt64(line, idx + 2);
                        break;
                    case nameof(ParsedStatus.VmHWM):
                        result.VmHWM = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.VmRSS):
                        result.VmRSS = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.RssAnon):
                        result.RssAnon = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.RsSFile):
                        result.RsSFile = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.RssShmem):
                        result.RssShmem = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.VmData):
                        result.VmData = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.VmSwap):
                        result.VmSwap = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.VmSize):
                        result.VmSize = ReadUInt64(line, idx + 2) * 1024;
                        break;
                    case nameof(ParsedStatus.VmPeak):
                        result.VmPeak = ReadUInt64(line, idx + 2) * 1024;
                        break;
                }
            }

            return true;
        }

        private static ulong ReadUInt64(string line, int startIndex)
        {
            ulong result = 0;

            while (line[startIndex] == ' ')
            {
                ++startIndex;
            }

            int len = line.Length;

            while (startIndex < len)
            {
                char c = line[startIndex];

                int d = c - '0';

                if (d >= 0 && d <= 9)
                {
                    result = checked((result * 10ul) + (ulong)d);
                    ++startIndex;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private static long ReadInt64(string line, int startIndex)
        {
            long result = 0;

            while (line[startIndex] == ' ')
            {
                ++startIndex;
            }

            int len = line.Length;

            while (startIndex < len)
            {
                char c = line[startIndex];

                int d = c - '0';

                if (d >= 0 && d <= 9)
                {
                    result = checked((result * 10L) + (long)d);
                    ++startIndex;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private static bool TryReadFile(string filePath, out string[] fileLines)
        {
            try
            {
                fileLines = File.ReadAllLines(filePath);
                return true;
            }
            catch (IOException)
            {
                fileLines = null;
                return false;
            }
        }
    }
}