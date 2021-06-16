using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Hugin.Hubs;
using Hugin.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Hugin.Services
{

    public partial class SandboxExecutionService
    {
        private readonly IServiceScopeFactory ServiceScopeFactory;
        public readonly IBackgroundTaskQueueSet Queues;
        public readonly SandboxHandleService SandboxHandler;
        public readonly FilePathResolveService FilePathResolver;
        public readonly RepositoryHandleService RepositoryHandler;
        public readonly SandboxNotifierService SandboxNotifier;

        private readonly ConcurrentDictionary<string, object> locks;

        private readonly IHubContext<ActivityHub> HubContext;

        public SandboxExecutionService(IServiceScopeFactory serviceScopeFactory,
            SandboxNotifierService sandboxNotifier,
            IBackgroundTaskQueueSet queues,
            SandboxHandleService sandboxHandler,
            FilePathResolveService filePathResolver,
            RepositoryHandleService repositoryHandler,
            IHubContext<ActivityHub> hubContext)
        {
            ServiceScopeFactory = serviceScopeFactory;
            SandboxNotifier = sandboxNotifier;
            Queues = queues;
            SandboxHandler = sandboxHandler;
            FilePathResolver = filePathResolver;
            HubContext = hubContext;
            RepositoryHandler = repositoryHandler;
            locks = new ConcurrentDictionary<string, object>();
        }

        public void Install(
            Data.User user,
            Data.Sandbox sandbox,
            string installerCommands = null,
            Func<string, Task> stdoutCallback = null,
            Func<string, Task> stderrCallback = null,
            Func<Task> doneCallback = null)
        {
            var directorypath = FilePathResolver.GetSandboxDirectoryPath(user.Account, sandbox.Lecture.Name, sandbox.Name);

            if (sandbox.State == Sandbox.SandboxState.Uninstalled && string.IsNullOrWhiteSpace(installerCommands))
            {
                var run = $@"debootstrap stretch {directorypath} http://http.debian.net/debian;";
                var desc = $"Install sandbox {user.Account}/{sandbox.Lecture.Name}/{sandbox.Name}";
                Queues.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                            sandbox.State = Sandbox.SandboxState.Installing;
                            handler.Update(sandbox);
                        }

                        await ExecuteAsync(user, null, sudo: true, stdin: run,
                            stdoutCallback: (_, x) => stdoutCallback?.Invoke(x),
                            stderrCallback: (_, x) => stderrCallback?.Invoke(x));

                        File.Copy($"{directorypath}/etc/passwd", $"{directorypath}/etc/passwd.original", true);
                        File.Copy($"{directorypath}/etc/group", $"{directorypath}/etc/group.original", true);

                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                            sandbox.State = Sandbox.SandboxState.Installed;
                            handler.Update(sandbox);
                        }

                        doneCallback().Wait();
                    }
                    catch(Exception)
                    {
                        doneCallback().Wait();
                    }
                    await SandboxNotifier.Update();
                }, user, desc, user.IsTeacher);
            }
            else if (sandbox.State != Sandbox.SandboxState.Uninstalled && string.IsNullOrWhiteSpace(installerCommands))
            {
                doneCallback().Wait();
            }
            else if (sandbox.State != Sandbox.SandboxState.Uninstalled && !string.IsNullOrWhiteSpace(installerCommands))
            {
                var run = $@"debootstrap stretch {directorypath} http://http.debian.net/debian;";
                var desc = $"Install sandbox {user.Account}/{sandbox.Lecture.Name}/{sandbox.Name}";
                Queues.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                            sandbox.State = Sandbox.SandboxState.Installing;
                            handler.Update(sandbox);
                        }

                        await ExecuteAsync(user, sandbox, sudo: true,
                            stdin: installerCommands.Replace("\r\n", Environment.NewLine).Replace("\r", Environment.NewLine).Replace("\n", Environment.NewLine),
                            stdoutCallback: (_, x) => stdoutCallback?.Invoke(x),
                            stderrCallback: (_, x) => stderrCallback?.Invoke(x));

                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                            sandbox.State = Sandbox.SandboxState.Installed;
                            handler.Update(sandbox);
                        }

                        doneCallback().Wait();
                    }
                    catch (Exception)
                    {
                        doneCallback().Wait();
                    }
                    await SandboxNotifier.Update();
                }, user, desc, user.IsTeacher);
            }
            else if(sandbox.State == Sandbox.SandboxState.Uninstalled && !string.IsNullOrWhiteSpace(installerCommands))
            {
                var run = $@"debootstrap stretch {directorypath} http://http.debian.net/debian;";
                var desc = $"Install sandbox {user.Account}/{sandbox.Lecture.Name}/{sandbox.Name}";
                Queues.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                            sandbox.State = Sandbox.SandboxState.Installing;
                            handler.Update(sandbox);
                        }

                        await ExecuteAsync(user, null, sudo: true, stdin: run,
                            stdoutCallback: (_, x) => stdoutCallback?.Invoke(x),
                            stderrCallback: (_, x) => stderrCallback?.Invoke(x));

                        File.Copy($"{directorypath}/etc/passwd", $"{directorypath}/etc/passwd.original", true);
                        File.Copy($"{directorypath}/etc/group", $"{directorypath}/etc/group.original", true);

                        await ExecuteAsync(user, sandbox, sudo: true,
                            stdin: installerCommands.Replace("\r\n", Environment.NewLine).Replace("\r", Environment.NewLine).Replace("\n", Environment.NewLine),
                            stdoutCallback: (_, x) => stdoutCallback?.Invoke(x),
                            stderrCallback: (_, x) => stderrCallback?.Invoke(x));

                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                            sandbox.State = Sandbox.SandboxState.Installed;
                            handler.Update(sandbox);
                        }

                        doneCallback().Wait();
                    }
                    catch (Exception)
                    {
                        doneCallback().Wait();
                    }
                    await SandboxNotifier.Update();
                }, user, desc, user.IsTeacher);
            }
        }

        public void EnqueueExecution(
            Data.User user,
            Data.Sandbox sandbox,
            string description = null,
            string stdin = null,
            Func<string, Task> stdoutCallback = null,
            Func<string, Task> stderrCallback = null,
            Func<string, Task> cmdCallback = null,
            Func<string, Task> summaryCallback = null,
            Func<int, Task> doneCallback = null,
            Models.ResourceLimits limit = null,
            bool sudo = false)
        {
            Queues.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    await ExecuteAsync(user, sandbox, "/bin/bash", null, stdin,
                        (_, x) => stdoutCallback?.Invoke(x),
                        (_, x) => stderrCallback?.Invoke(x),
                        (_, x) => cmdCallback?.Invoke(x),
                        (_, x) => summaryCallback?.Invoke(x),
                        (_, x) => doneCallback?.Invoke(x),
                        limit, sudo);
                }
                catch (Exception e)
                {
                    stderrCallback?.Invoke(e.Message);
                    doneCallback?.Invoke(-1);
                }
            }, user, description, user.IsTeacher);
        }

        public void EnqueueExecution(
            Data.User user,
            Data.Sandbox sandbox,
            string description = null,
            string stdin = null,
            Func<IHubContext<ActivityHub>, string, Task> stdoutCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> stderrCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> cmdCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> summaryCallback = null,
            Func<IHubContext<ActivityHub>, Task<string>> stdinCallback = null,
            Func<IHubContext<ActivityHub>, int, Task> doneCallback = null,
            Models.ResourceLimits limit = null,
            bool sudo = false)
        {
            Queues.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    await ExecuteActivityAsync(user, sandbox, stdin, stdoutCallback, stderrCallback, cmdCallback, summaryCallback, stdinCallback, doneCallback, limit, sudo, false);
                }
                catch(Exception e)
                {
                    stderrCallback?.Invoke(HubContext, e.Message);
                    doneCallback?.Invoke(HubContext, -1);
                }
            }, user, description, user.IsTeacher);
        }

        public void MountIfUnmounted(Data.User user, Data.Sandbox sandbox)
        {
            var directoryPath = FilePathResolver.GetSandboxDirectoryPath(sandbox);

            doWithLock(sandbox, s =>
            {
                var proc1 = Process.Start("/bin/bash", $"-c \"HOME=/ chroot --userspec root:root {directoryPath} /bin/bash -c 'id {user.Account} > /dev/null 2> /dev/null'\"");
                proc1.WaitForExit();
                if (proc1.ExitCode != 0)
                {

                    var sb1 = new System.Text.StringBuilder();
                    var sb2 = new System.Text.StringBuilder();

                    using (var r = new StreamReader($"{directoryPath}/etc/passwd"))
                    {
                        sb1.AppendLine(r.ReadToEnd());
                    }
                    sb1.AppendLine($"{user.Account}:x:{user.Uid + 1000}:{user.Uid + 1000}:{user.Account}:/home/{user.Account}:/bin/bash");
                    using (var w = new StreamWriter($"{directoryPath}/etc/passwd"))
                    {
                        w.Write(sb1.ToString());
                    }

                    using (var r = new StreamReader($"{directoryPath}/etc/group"))
                    {
                        sb2.AppendLine(r.ReadToEnd());
                    }
                    sb2.AppendLine($"{user.Account}:x:{user.Uid + 1000}:");
                    using (var w = new StreamWriter($"{directoryPath}/etc/group"))
                    {
                        w.Write(sb2.ToString());
                    }
                }
            });

            var proc2 = Process.Start("/bin/bash", $"-c \"mountpoint -q {directoryPath}/home/{user.Account} > /dev/null 2> /dev/null\"");
            proc2.WaitForExit();
            if (proc2.ExitCode != 0)
            {
                var h = new DirectoryInfo($"{directoryPath}/home/{user.Account}");
                if (!h.Exists)
                {
                    h.Create();
                }
                try
                {
                    var f = $"{FilePathResolver.GetLectureUserDataDirectoryPath(sandbox.Lecture, user.Account, "master")}/home";
                    Process.Start("chmod", $"700 {f}").WaitForExit();
                    Process.Start("chown", $"{user.Id + 1000}:{user.Id + 1000} {f}").WaitForExit();
                    Process.Start("mount", $"--bind {f} {h.FullName}").WaitForExit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        public async Task ExecuteAsync(
            Data.User user,
            Data.Sandbox sandbox,
            string program = "/bin/bash",
            string args = "",
            string stdin = null,
            Func<IHubContext<ActivityHub>, string, Task> stdoutCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> stderrCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> cmdCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> summaryCallback = null,
            Func<IHubContext<ActivityHub>, int, Task> doneCallback = null,
            Models.ResourceLimits limit = null,
            bool sudo = false)
        {
            bool onSandbox = sandbox != null;
            if (!sudo && onSandbox)
            {
                MountIfUnmounted(user, sandbox);
            }

            await Task.Run(() => {
                var directoryPath = onSandbox ? FilePathResolver.GetSandboxDirectoryPath(sandbox) : "";
                var userId = user.Uid + 1000;
                var fifoname1 = Guid.NewGuid().ToString("N").Substring(0, 32);
                var fifoname2 = Guid.NewGuid().ToString("N").Substring(0, 32);

                if (onSandbox)
                {
                    Process.Start("mkfifo", $"{directoryPath}/var/tmp/{fifoname1}").WaitForExit();
                    Process.Start("chown", $"{userId} {directoryPath}/var/tmp/{fifoname1}").WaitForExit();
                    Process.Start("mkfifo", $"{directoryPath}/var/tmp/{fifoname2}").WaitForExit();
                    Process.Start("chown", $"{userId} {directoryPath}/var/tmp/{fifoname2}").WaitForExit();
                }

                var (account, home) = sudo ? ("root", "/") : (user.Account, $"/home/{user.Account}");

                (program, args) = (limit, onSandbox) switch
                {
                    (null, true)
                    => ("/bin/bash", $"-c \"HOME={home} chroot --userspec {account}:{account} {directoryPath} {program} {args}\""),
                    (Models.ResourceLimits, true)
                    => ("/bin/bash", $"-c \"{makeUlimitCommand(limit, $"HOME={home}")} chroot --userspec {account}:{account} {directoryPath} {program} {args}\""),
                    _ => (program, args),
                };


                var mainProc = Task.Run(() => {
                    var proc = new Process();
                    proc.StartInfo.FileName = "stdbuf";
                    proc.StartInfo.Arguments = $"-o0 -e0 -i0 {program} {args}";

                    proc.StartInfo.Environment["CMD"] = $"/var/tmp/{fifoname1}";
                    proc.StartInfo.Environment["SUMMARY"] = $"/var/tmp/{fifoname2}";

                    proc.StartInfo.RedirectStandardInput = true;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;

                    var errorClosed = new ManualResetEvent(false);
                    var outputClosed = new ManualResetEvent(false);
                    ManualResetEvent[] waits = { outputClosed, errorClosed };
                    errorClosed.Reset();
                    outputClosed.Reset();

                    if (stdoutCallback != null)
                    {
                        if (limit?.StdoutLength != null && limit.StdoutLength > 0)
                        {
                            var remaind = limit.StdoutLength;
                            proc.OutputDataReceived += (o, e) =>
                            {
                                if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                {
                                    if (e.Data.Length <= remaind)
                                    {
                                        stdoutCallback?.Invoke(HubContext, e.Data);
                                        remaind -= (uint)e.Data.Length;
                                    }
                                    else
                                    {
                                        stdoutCallback?.Invoke(HubContext, e.Data.Substring(0, (int)remaind));
                                        remaind = 0;
                                    }
                                }
                                outputClosed.Set();
                            };
                        }
                        else
                        {
                            proc.OutputDataReceived += (o, e) => { stdoutCallback?.Invoke(HubContext, e.Data); outputClosed.Set(); };
                        }
                    }
                    else
                    {
                        outputClosed.Set();
                    }
                    if (stderrCallback != null)
                    {
                        if (limit?.StderrLength != null && limit.StderrLength > 0)
                        {
                            var remaind = limit.StderrLength;
                            proc.ErrorDataReceived += (o, e) =>
                            {
                                if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                {
                                    if (e.Data.Length <= remaind)
                                    {
                                        stderrCallback?.Invoke(HubContext, e.Data);
                                        remaind -= (uint)e.Data.Length;
                                    }
                                    else
                                    {
                                        stderrCallback?.Invoke(HubContext, e.Data.Substring(0, (int)remaind));
                                        remaind = 0;
                                    }
                                }
                                errorClosed.Set();
                            };
                        }
                        else
                        {
                            proc.ErrorDataReceived += (o, e) => { stderrCallback?.Invoke(HubContext, e.Data); errorClosed.Set(); };
                        }
                    }
                    else
                    {
                        errorClosed.Set();
                    }
                    proc.Start();
                    try
                    {
                        proc.StandardInput.WriteLine(stdin);
                        proc.StandardInput.Close();
                        if (stdoutCallback != null) { proc.BeginOutputReadLine(); }
                        if (stderrCallback != null) { proc.BeginErrorReadLine(); }
                        var waitTime = 10000;
                        if (limit != null)
                        {
                            waitTime = (int)(limit.CpuTime + 5) * 1000;
                            proc.WaitForExit(waitTime);
                        }
                        else
                        {
                            proc.WaitForExit();
                        }
                        if (!ManualResetEvent.WaitAll(waits, waitTime))
                        {
                            var errorMsg = $"Hugin System error: STDOUT/ERR wait timeout";
                            stderrCallback?.Invoke(HubContext, errorMsg);
                        }
                        doneCallback?.Invoke(HubContext, proc.ExitCode);
                    }
                    catch (Exception e)
                    {
                        stderrCallback?.Invoke(HubContext, e?.Message);
                        doneCallback?.Invoke(HubContext, -1);
                    }
                    finally
                    {
                        proc.Close();
                    }
                });

                try
                {
                    var tokenSource = new CancellationTokenSource();
                    var token = tokenSource.Token;
                    if (onSandbox && File.Exists($"{directoryPath}/var/tmp/{fifoname1}"))
                    {
                        var observer = Task.Run(() =>
                        {
                            using (var r = new StreamReader($"{directoryPath}/var/tmp/{fifoname1}"))
                            {
                                while (!token.IsCancellationRequested)
                                {
                                    if (!r.EndOfStream)
                                    {
                                        var t = r.ReadLineAsync();
                                        t.Wait(token);
                                        if (t.Result != null)
                                        {
                                            cmdCallback?.Invoke(HubContext, t.Result);
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }, token);
                    }
                    if (onSandbox && File.Exists($"{directoryPath}/var/tmp/{fifoname2}"))
                    {
                        var observer = Task.Run(() =>
                        {
                            using (var r = new StreamReader($"{directoryPath}/var/tmp/{fifoname2}"))
                            {
                                while (!token.IsCancellationRequested)
                                {
                                    if (!r.EndOfStream)
                                    {
                                        var t = r.ReadLineAsync();
                                        t.Wait(token);
                                        if (t.Result != null)
                                        {
                                            summaryCallback?.Invoke(HubContext, t.Result);
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }, token);
                    }

                    mainProc.Wait();

                    tokenSource.Cancel();
                }
                catch (Exception e)
                {
                    stderrCallback?.Invoke(HubContext, e?.Message);
                    doneCallback?.Invoke(HubContext, -1);
                }
                finally
                {
                    if (onSandbox)
                    {
                        Process.Start("rm", $"{directoryPath}/var/tmp/{fifoname1}").WaitForExit();
                        Process.Start("rm", $"{directoryPath}/var/tmp/{fifoname2}").WaitForExit();
                    }
                }
            });
        }

        public async Task ExecuteActivityAsync(
            Data.User user,
            Data.Sandbox sandbox,
            string stdin = null,
            Func<IHubContext<ActivityHub>, string, Task> stdoutCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> stderrCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> cmdCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> summaryCallback = null,
            Func<IHubContext<ActivityHub>, Task<string>> stdinCallback = null,
            Func<IHubContext<ActivityHub>, int, Task> doneCallback = null,
            Models.ResourceLimits limit = null,
            bool sudo = false,
            bool lineBuffered = true)
        {
            bool onSandbox = sandbox != null;
            if (!sudo && onSandbox)
            {
                MountIfUnmounted(user, sandbox);
            }

            await Task.Run(() => {
                var directoryPath = onSandbox ? FilePathResolver.GetSandboxDirectoryPath(sandbox) : "";
                var userId = user.Uid + 1000;
                var r = Guid.NewGuid().ToString("N").Substring(0, 32);
                var fifoname0 = $"{r}-pg";
                var fifoname1 = $"{r}-cmd";
                var fifoname2 = $"{r}-smr";

                if (onSandbox)
                {
                    Process.Start("mkfifo", $"{directoryPath}/var/tmp/{fifoname0}").WaitForExit();
                    Process.Start("chown", $"{userId} {directoryPath}/var/tmp/{fifoname0}").WaitForExit();
                    Process.Start("mkfifo", $"{directoryPath}/var/tmp/{fifoname1}").WaitForExit();
                    Process.Start("chown", $"{userId} {directoryPath}/var/tmp/{fifoname1}").WaitForExit();
                    Process.Start("mkfifo", $"{directoryPath}/var/tmp/{fifoname2}").WaitForExit();
                    Process.Start("chown", $"{userId} {directoryPath}/var/tmp/{fifoname2}").WaitForExit();
                    Process.Start("ls", $"-la {directoryPath}/var/tmp/").WaitForExit();
                }

                Console.WriteLine($"chown {userId} {directoryPath}/var/tmp/{fifoname0}");

                var (account, home) = sudo ? ("root", "/") : (user.Account, $"/home/{user.Account}");

                var (program, args) = (limit, onSandbox) switch
                {
                    (null, true)
                    => ("/bin/bash", $"-c \"HOME={home} chroot --userspec {account}:{account} {directoryPath} /bin/bash /var/tmp/{fifoname0}\""),
                    (Models.ResourceLimits, true)
                    => ("/bin/bash", $"-c \"{makeUlimitCommand(limit, $"HOME={home}")} chroot --userspec {account}:{account} {directoryPath} /bin/bash /var/tmp/{fifoname0}\""),
                    _ => ("/bin/bash", $"/var/tmp/{fifoname0}"),
                };

                var mainProc = Task.Run(() => {
                    var proc = new Process();
                    proc.StartInfo.FileName = "stdbuf";
                    proc.StartInfo.Arguments = $"-o0 -e0 -i0 {program} {args}";

                    proc.StartInfo.Environment["CMD"] = $"/var/tmp/{fifoname1}";
                    proc.StartInfo.Environment["SUMMARY"] = $"/var/tmp/{fifoname2}";

                    proc.StartInfo.RedirectStandardInput = true;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.EnableRaisingEvents = true;


                    var errorClosed = new ManualResetEvent(false);
                    var outputClosed = new ManualResetEvent(false);
                    ManualResetEvent[] waits = { outputClosed, errorClosed };
                    errorClosed.Reset();
                    outputClosed.Reset();

                    if(lineBuffered)
                    {
                        if (stdoutCallback != null)
                        {
                            if (limit?.StdoutLength != null && limit.StdoutLength > 0)
                            {
                                var remaind = limit.StdoutLength;
                                proc.OutputDataReceived += (o, e) =>
                                {
                                    if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                    {
                                        if (e.Data.Length <= remaind)
                                        {
                                            stdoutCallback?.Invoke(HubContext, e.Data);
                                            remaind -= (uint)e.Data.Length;
                                        }
                                        else
                                        {
                                            stdoutCallback?.Invoke(HubContext, e.Data.Substring(0, (int)remaind));
                                            remaind = 0;
                                        }
                                    }
                                    outputClosed.Set();
                                };
                            }
                            else
                            {
                                proc.OutputDataReceived += (o, e) => { stdoutCallback?.Invoke(HubContext, e.Data); outputClosed.Set(); };
                            }
                        }
                        else
                        {
                            outputClosed.Set();
                        }
                        if (stderrCallback != null)
                        {
                            if (limit?.StderrLength != null && limit.StderrLength > 0)
                            {
                                var remaind = limit.StderrLength;
                                proc.ErrorDataReceived += (o, e) =>
                                {
                                    if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                    {
                                        if (e.Data.Length <= remaind)
                                        {
                                            stderrCallback?.Invoke(HubContext, e.Data);
                                            remaind -= (uint)e.Data.Length;
                                        }
                                        else
                                        {
                                            stderrCallback?.Invoke(HubContext, e.Data.Substring(0, (int)remaind));
                                            remaind = 0;
                                        }
                                    }
                                    errorClosed.Set();
                                };
                            }
                            else
                            {
                                proc.ErrorDataReceived += (o, e) => { stderrCallback?.Invoke(HubContext, e.Data); errorClosed.Set(); };
                            }
                        }
                        else
                        {
                            errorClosed.Set();
                        }
                    }
                    proc.Start();

                    if (!lineBuffered)
                    {
                        Task.Run(() =>
                        {
                            var remaind = limit == null ? 0 : limit.StdoutLength;
                            var count = 0;
                            var sb = new System.Text.StringBuilder();
                            while (!proc.StandardOutput.EndOfStream)
                            {
                                var d = proc.StandardOutput.Read();
                                if (d >= 0)
                                {
                                    if (remaind > 0)
                                    {
                                        count++;
                                        if(remaind <= count)
                                        {
                                            break;
                                        }
                                    }

                                    var c = (char)d;
                                    sb.Append(c);
                                    if (!Char.IsHighSurrogate(c))
                                    {
                                        stdoutCallback?.Invoke(HubContext, sb.ToString());
                                        sb.Clear();
                                    }
                                }
                            }
                            outputClosed.Set();
                        });
                        Task.Run(() =>
                        {
                            var remaind = limit == null ? 0 : limit.StderrLength;
                            var count = 0;
                            var sb = new System.Text.StringBuilder();
                            while (!proc.StandardError.EndOfStream)
                            {
                                var d = proc.StandardError.Read();
                                if (d >= 0)
                                {
                                    if (remaind > 0)
                                    {
                                        count++;
                                        if (remaind <= count)
                                        {
                                            break;
                                        }
                                    }

                                    var c = (char)d;
                                    sb.Append(c);
                                    if (!Char.IsHighSurrogate(c))
                                    {
                                        stderrCallback?.Invoke(HubContext, sb.ToString());
                                        sb.Clear();
                                    }
                                }
                            }
                            errorClosed.Set();
                        });
                    }

                    try
                    {
                        var p = new Process();
                        p.StartInfo.FileName = "/bin/bash";
                        p.StartInfo.Arguments = $"-c \"cat - > {directoryPath}/var/tmp/{fifoname0}\"";
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.Start();
                        p.StandardInput.WriteLine(stdin);
                        p.StandardInput.Close();
                        p.WaitForExit();
                        p.Close();

                        if (lineBuffered)
                        {
                            if (stdoutCallback != null) { proc.BeginOutputReadLine(); }
                            if (stderrCallback != null) { proc.BeginErrorReadLine(); }
                        }
                        bool running = true;
                        if (stdinCallback != null)
                        {
                            Task.Run(async () => {
                                try
                                {
                                    while (!proc.HasExited && running)
                                    {
                                        proc.StandardInput.WriteLine(await stdinCallback(HubContext));
                                    }
                                }
                                catch (Exception e)
                                {
                                }
                            });
                        }
                        proc.Exited += (_, _) =>
                        {
                            running = false;
                        };

                        var waitTime = 10000;
                        if (limit != null)
                        {
                            waitTime = (int)(limit.CpuTime + 5) * 1000;
                            proc.WaitForExit(waitTime);
                        }
                        else
                        {
                            proc.WaitForExit();
                        }
                        proc.StandardInput.Close();
                        if (!ManualResetEvent.WaitAll( waits, waitTime))
                        {
                            var errorMsg = $"Hugin System error: STDOUT/ERR wait timeout";
                            stderrCallback?.Invoke(HubContext, errorMsg);
                        }
                        doneCallback?.Invoke(HubContext, proc.ExitCode);
                    }
                    catch (Exception e)
                    {
                        stderrCallback?.Invoke(HubContext, e?.Message);
                        doneCallback?.Invoke(HubContext, -1);
                    }
                    finally
                    {
                        proc.Close();
                    }
                });

                try
                {
                    var tokenSource = new CancellationTokenSource();
                    var token = tokenSource.Token;

                    if (onSandbox && File.Exists($"{directoryPath}/var/tmp/{fifoname1}"))
                    {
                        var observer = Task.Run(() =>
                        {
                            using (var r = new StreamReader($"{directoryPath}/var/tmp/{fifoname1}"))
                            {
                                while (!token.IsCancellationRequested)
                                {
                                    if (!r.EndOfStream)
                                    {
                                        var t = r.ReadLineAsync();
                                        t.Wait(token);
                                        if (t.Result != null)
                                        {
                                            cmdCallback?.Invoke(HubContext, t.Result);
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }, token);
                    }
                    if (onSandbox && File.Exists($"{directoryPath}/var/tmp/{fifoname2}"))
                    {
                        var observer = Task.Run(() =>
                        {
                            using (var r = new StreamReader($"{directoryPath}/var/tmp/{fifoname2}"))
                            {
                                while (!token.IsCancellationRequested)
                                {
                                    if (!r.EndOfStream)
                                    {
                                        var t = r.ReadLineAsync();
                                        t.Wait(token);
                                        if (t.Result != null)
                                        {
                                            summaryCallback?.Invoke(HubContext, t.Result);
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }, token);
                    }

                    mainProc.Wait();

                    tokenSource.Cancel();
                }
                catch (Exception e)
                {
                    stderrCallback?.Invoke(HubContext, e?.Message);
                    doneCallback?.Invoke(HubContext, -1);
                }
                finally
                {
                    if (onSandbox)
                    {
                        Process.Start("rm", $"{directoryPath}/var/tmp/{fifoname0}").WaitForExit();
                        Process.Start("rm", $"{directoryPath}/var/tmp/{fifoname1}").WaitForExit();
                        Process.Start("rm", $"{directoryPath}/var/tmp/{fifoname2}").WaitForExit();
                    }

                }
            });
        }



        private string makeUlimitCommand(Models.ResourceLimits limit, string midstgring = "")
        {
            var sb = new System.Text.StringBuilder();
            bool useCpuTime = false;

            if (limit.CpuTime > 0)
            {
                sb.Append($"ulimit -t {limit.CpuTime};");
                useCpuTime = true;
            }

            if (limit.Pids > 0)
            {
                sb.Append($"ulimit -u {limit.Pids};");
                useCpuTime = true;
            }

            if (!string.IsNullOrWhiteSpace(limit.Memory))
            {
                var regex = new System.Text.RegularExpressions.Regex("^(?<dec>[0-9.]+)(?<uni>(|K|M|G|k|m|g))$");
                var m = regex.Match(limit.Memory);
                if (!m.Groups["dec"].Success || !decimal.TryParse(m.Groups["dec"].Value, out var value))
                {
                    value = 0;
                }
                var unit = m.Groups["uni"].Success ? m.Groups["uni"].Value?.ToLower() : "";
                if (unit == "k")
                {
                    value = value * 1024;
                }
                if (unit == "m")
                {
                    value = value * 1024 * 1024;
                }
                if (unit == "g")
                {
                    value = value * 1024 * 1024 * 1024;
                }
                sb.Append($"ulimit -m {value}; ulimit -v {value};");
            }

            return useCpuTime ? $"{sb.ToString()} {midstgring} timeout {limit.CpuTime} " : $"{sb.ToString()} {midstgring}";
        }






        private void doWithLock(Data.Sandbox sandbox, Action<Data.Sandbox> action)
        {
            object lk;
            lock (locks)
            {
                lk = locks.GetOrAdd(FilePathResolver.GetSandboxDirectoryPath(sandbox), (key) => new object());
            }
            lock (lk)
            {
                action.Invoke(sandbox);
            }
        }
        private T doWithLock<T>(Data.Sandbox sandbox, Func<Data.Sandbox, T> action)
        {
            object lk;
            lock (locks)
            {
                lk = locks.GetOrAdd(FilePathResolver.GetSandboxDirectoryPath(sandbox), (key) => new object());
            }
            lock (lk)
            {
                return action.Invoke(sandbox);
            }
        }
    }


}
