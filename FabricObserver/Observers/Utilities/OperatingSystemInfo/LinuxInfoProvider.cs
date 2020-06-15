﻿using System.IO;
using System.Text;

namespace FabricObserver.Observers.Utilities
{
    internal class LinuxInfoProvider : OperatingSystemInfoProvider
    {
        internal override (long TotalMemory, int PercentInUse) TupleGetTotalPhysicalMemorySizeAndPercentInUse()
        {
            long totalMemory = -1;

            using (StreamReader sr = new StreamReader("/proc/meminfo", encoding: Encoding.ASCII))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        totalMemory = ReadInt64(line, "MemTotal:".Length + 1);
                    }
                    else if (line.StartsWith("MemFree:"))
                    {
                        long freeMem = ReadInt64(line, "MemFree:".Length + 1);

                        return (totalMemory / 1024, (int)(((double)(totalMemory - freeMem)) / totalMemory * 100));
                    }
                }
            }

            return (-1L, -1);
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
    }
}