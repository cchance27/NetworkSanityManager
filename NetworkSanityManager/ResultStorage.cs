using Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSanityManager
{
    internal static class ResultStorage
    {
        private static Configuration _config;

        internal static void SaveResults(Device[] deviceData)
        {
            _config = Program._config;

            var success = deviceData.Where(r => String.IsNullOrWhiteSpace(r.Errors)).ToArray<Device>();
            var successCsv = GenerateCSV(success);
            SaveCsv(successCsv);

            var failures = deviceData.Where(r => String.IsNullOrWhiteSpace(r.Errors) == false).ToArray<Device>();

            if (failures.Length > 0)
            {
                var failCsv = GenerateCSV(failures);
                SaveCsv(failCsv, "-failures");
            }
        }

        private static void SaveCsv(string csvString, string suffix = "")
        {
            var fileToSave = $".\\{DateTime.Today.ToString("yyyy-MM-dd")}{suffix}.csv";
            StreamWriter file = new System.IO.StreamWriter(fileToSave);
            file.Write(csvString);
            file.Close();
            Console.WriteLine($"CSV Saved: {fileToSave}");
        }

        private static string GenerateCSV(Device[] data)
        {
            var formatter = new CsvFormatter();
            var buffer = new StringBuilder();
            buffer.Append(formatter.FormatTitle(typeof(Device)));
            buffer.Append("\n");
            foreach (Device item in data)
            {
                try
                {
                    buffer.Append(formatter.FormatItem(item) + '\n');
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Buffer Append Failed: {item} ({e.InnerException})");
                }
            }
            return buffer.ToString();
        }
    }
}
