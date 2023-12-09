// Author : rxndev
// github.com/rxndev
// velog.io/@rxndev

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WinKMDLoader
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("[WinKMDLoader]");

            if (args.Length == 1)
            {
                LoaderEntry(args[0]);
            }
            else if (args.Length == 3 && args[0].Equals("-unloader", StringComparison.CurrentCultureIgnoreCase) && int.TryParse(args[1], out int loaderProcessId))
            {
                UnloaderEntry(loaderProcessId, args[2]);
            }
            else
            {
                ExitWithError("Usage : WinKMDLoader.exe <DRIVER_FILE_PATH>");
            }
        }

        private static void LoaderEntry(string driverFilePath)
        {
            string driverFileAbsolutePath = GetDriverFileAbsolutePath(driverFilePath);

            if (driverFileAbsolutePath == null)
            {
                ExitWithError("Invalid driver file path.");
            }

            if (!File.Exists(driverFileAbsolutePath))
            {
                ExitWithError("Driver file does not exist.");
            }

            string driverName = Path.GetFileNameWithoutExtension(driverFileAbsolutePath);

            Console.WriteLine($"Driver file path : {driverFileAbsolutePath}");
            Console.WriteLine($"Driver name : {driverName}");

            CreateUnloaderProcess(driverName);
            Console.WriteLine("Unloader process created successfully.");

            if (ExecuteBackgroundProcess("sc", $"create \"{driverName}\" type= kernel binpath= \"{driverFileAbsolutePath}\"") != 0)
            {
                ExitWithError("Failed to create service.");
            }

            Console.WriteLine("Service created successfully.");

            if (ExecuteBackgroundProcess("sc", $"start \"{driverName}\"") != 0)
            {
                ExitWithError("Failed to start service.");
            }

            Console.WriteLine("Service started successfully.");

            while (true)
            {
                Console.ReadKey(true);
            }
        }

        private static void UnloaderEntry(int loaderProcessId, string driverName)
        {
            using Process loaderProcess = Process.GetProcessById(loaderProcessId);
            loaderProcess.WaitForExit();
            ExecuteBackgroundProcess("sc", $"stop \"{driverName}\"");
            ExecuteBackgroundProcess("sc", $"delete \"{driverName}\"");
        }

        private static string GetDriverFileAbsolutePath(string driverFilePath)
        {
            try
            {
                return Path.GetFullPath(driverFilePath);
            }
            catch
            {
                return null;
            }
        }

        private static void CreateUnloaderProcess(string driverName)
        {
            using Process currentProcess = Process.GetCurrentProcess();
            ExecuteBackgroundProcess(Assembly.GetExecutingAssembly().Location, $"-unloader {currentProcess.Id} {driverName}", false);
        }

        private static void ExitWithError(string errorMessage)
        {
            Console.WriteLine($"[Error] {errorMessage}");
            Environment.Exit(1);
        }

        private static int ExecuteBackgroundProcess(string fileName, string arguments, bool waitForExit = true)
        {
            using Process process = new()
            {
                StartInfo = new()
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            if (waitForExit)
            {
                process.WaitForExit();
                return process.ExitCode;
            }
            else
            {
                return -1;
            }
        }
    }
}