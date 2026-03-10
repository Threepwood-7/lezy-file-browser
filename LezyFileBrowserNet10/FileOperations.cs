using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LezyFileBrowser
{
    public class FileOpEventArgs : EventArgs
    {
        public bool IsSuccessful { get; set; }
        public DateTime CompletionTime { get; set; }
        public string Message { get; set; }
    }

    public class DirMoveOperation
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool MoveWithRobocopy { get; set; }
    }

    public class FileOperations
    {
        public EventHandler<FileOpEventArgs> FileOpEvent; // event

        private Queue<DirMoveOperation> moveOperations = new Queue<DirMoveOperation>();
        private HashSet<string> moveOperationsHistory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object historyLock = new object();

        private Thread operationsProcessor;

        private string inputDirNormalized;
        private string historyFile;
        private int queuePollMs;

        public FileOperations(string inputDir, int queuePollMs)
        {
            this.queuePollMs = queuePollMs;
            inputDirNormalized = inputDir.Replace(':', '_').Replace('\\', '_').Replace('/', '_').Replace(' ', '_');
            historyFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LZ_FM_HIST_" + inputDirNormalized + ".txt");

            operationsProcessor = new Thread(new ThreadStart(DoProcess));
            operationsProcessor.IsBackground = true;
            operationsProcessor.Start();
        }

        public void Enqueue(DirMoveOperation moveOperation)
        {
            lock (moveOperations)
            {
                moveOperations.Enqueue(moveOperation);
            }
        }

        private void DoProcess()
        {
            while (true)
            {
                DirMoveOperation moveOperation = null;

                lock (moveOperations)
                {
                    if (moveOperations.Count > 0)
                        moveOperation = moveOperations.Dequeue();
                }

                if (moveOperation != null)
                {
                    FileOpEvent?.Invoke(this, new FileOpEventArgs() { Message = $"* Starting move OP       [ {moveOperation.Source} ] > [ {moveOperation.Destination} ]" });
                    PerformOperation(moveOperation);
                }
                else
                {
                    Thread.Sleep(queuePollMs);
                }
            }
        }

        private void PerformOperation(DirMoveOperation moveOperation)
        {
            if (moveOperation.MoveWithRobocopy)
            {
                var lines = new String[] {
                        "REM >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\r\n\r\n" +
                        string.Format(
                            "ROBOCOPY \"{0}\" \"{1}\" /S /J /MOVE\r\n\r\n",
                            moveOperation.Source,
                            moveOperation.Destination),
                        "REM <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\r\n"
                    };

                var uid = Path.GetRandomFileName().Replace(".", "");
                var cmdPath = Path.Combine(Path.GetTempPath(), $"Z_1D_{uid}.cmd");
                File.WriteAllLines(cmdPath, lines, Encoding.Default);

                using (Process p = new Process())
                {
                    ProcessStartInfo pi = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec"), $"/C \"{cmdPath}\"");
                    pi.UseShellExecute = false;
                    pi.CreateNoWindow = true;
                    p.StartInfo = pi;
                    p.Start();
                    p.WaitForExit();

                    // Robocopy exit codes: 0=no change, 1=success, >=8=failure
                    bool ok = p.ExitCode < 8;
                    FileOpEvent?.Invoke(this, new FileOpEventArgs()
                    {
                        IsSuccessful = ok,
                        CompletionTime = DateTime.Now,
                        Message = ok
                            ? $"* Move OPR               [ {moveOperation.Source} ] complete, items in queue {moveOperations.Count}"
                            : $"* Move OPR FAILED        [ {moveOperation.Source} ] exit code {p.ExitCode}, items in queue {moveOperations.Count}"
                    });
                }
            }
            else
            {
                bool ok = false;
                try
                {
                    Directory.Move(moveOperation.Source, moveOperation.Destination);
                    ok = true;
                }
                catch (Exception ex)
                {
                    FileOpEvent?.Invoke(this, new FileOpEventArgs()
                    {
                        IsSuccessful = false,
                        CompletionTime = DateTime.Now,
                        Message = $"* Move OPC FAILED        [ {moveOperation.Source} ] {ex.Message}, items in queue {moveOperations.Count}"
                    });
                }

                if (ok)
                    FileOpEvent?.Invoke(this, new FileOpEventArgs()
                    {
                        IsSuccessful = true,
                        CompletionTime = DateTime.Now,
                        Message = $"* Move OPC               [ {moveOperation.Source} ] complete, items in queue {moveOperations.Count}"
                    });
            }
        }

        public void AddToHistory(string fileFullName)
        {
            lock (historyLock) { moveOperationsHistory.Add(fileFullName); }
        }

        public void RemoveFromHistory(string fileFullName)
        {
            lock (historyLock) { moveOperationsHistory.Remove(fileFullName); }
        }

        public bool IsInHistory(string fileFullName)
        {
            lock (historyLock) { return moveOperationsHistory.Contains(fileFullName); }
        }

        public void LoadHistory(string inputDir)
        {
            var _inputDir = inputDir.ToLower();

            if (File.Exists(historyFile))
            {
                var allLines = File.ReadAllLines(historyFile).Where(_ => _.ToLower().IndexOf(_inputDir) == 0).ToList();
                lock (historyLock) { foreach (var l in allLines) moveOperationsHistory.Add(l); }

                FileOpEvent?.Invoke(this, new FileOpEventArgs() { Message = $"* History File is        [ {historyFile} ] {allLines.Count} entries" });
            }
        }

        public void SaveHistory()
        {
            string[] snapshot;
            lock (historyLock) { snapshot = moveOperationsHistory.ToArray(); }
            File.WriteAllLines(historyFile, snapshot);
        }
    }
}
