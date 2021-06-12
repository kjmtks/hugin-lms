using Hugin.Data;
using Hugin.Hubs;
using Hugin.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class ActivityHandleService
    {
        private readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly IBackgroundTaskQueueSet Queues;
        private readonly ActivityActionHandleService ActivityActionHandler;
        private readonly UserHandleService UserHandler;
        private readonly LectureHandleService LectureHandler;
        private readonly ContentParserService ContentParser;
        private readonly ActivityEncryptService ActivityEncryptor;
        private readonly RepositoryHandleService RepositoryHandler;
        private readonly SandboxHandleService SandboxHandler;
        private readonly SandboxExecutionService Executor;
        public ActivityHandleService(IServiceScopeFactory serviceScopeFactory, IBackgroundTaskQueueSet queues, ActivityActionHandleService activityActionHandler, ActivityEncryptService activityEncryptor, SandboxExecutionService executor, UserHandleService userHandler, RepositoryHandleService repositoryHandler, LectureHandleService lectureHandler, SandboxHandleService sandboxHandler, ContentParserService contentParser)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Queues = queues;
            ActivityActionHandler = activityActionHandler;
            RepositoryHandler = repositoryHandler;
            LectureHandler = lectureHandler;
            UserHandler = userHandler;
            ContentParser = contentParser;
            ActivityEncryptor = activityEncryptor;
            Executor = executor;
            SandboxHandler = sandboxHandler;
        }

        public async Task<(ActivityProfile, Data.Lecture, Data.User, Models.Activity, string)> DecryptProfileAsync(string encryptedActivityProfile)
        {
            var profile = ActivityEncryptor.Decrypt(encryptedActivityProfile);
            var user = UserHandler.Set.Where(x => x.Account == profile.UserAccount).AsNoTracking().FirstOrDefault();
            var lecture = LectureHandler.Set.Include(x => x.Owner).Where(x => x.IsActived && x.Owner.Account == profile.LectureOwnerAccount && x.Name == profile.LectureName).AsNoTracking().FirstOrDefault();
            var (activity, xml) = await ContentParser.BuildActivityAsync(user, profile);

            return (profile, lecture, user, activity, xml);
        }
        public string SaveActivity(Data.Lecture lecture, Data.User user, Activity activity, string commitMessage,
            IDictionary<string, string> textfiles, IDictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles)
        {
            return save(lecture, user, activity, commitMessage, textfiles, binaryfiles, blocklyfiles);
        }

        public async Task<bool> SaveAndRunActivityAsync(Data.Lecture lecture, Data.User user, Activity activity, string commitMessageBeforeRun,
            IDictionary<string, string> textfiles, IDictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles,
            Func<Task> onSaveErrorOccurCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> stdoutCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> stderrCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> cmdCallback = null,
            Func<IHubContext<ActivityHub>, string, Task> summaryCallback = null,
            Func<IHubContext<ActivityHub>, int, Task> doneCallback = null)
        {
            var result = !string.IsNullOrWhiteSpace(save(lecture, user, activity, commitMessageBeforeRun, textfiles, binaryfiles, blocklyfiles));
            if(!result)
            {
                await onSaveErrorOccurCallback?.Invoke();
                return true;
            }
            else
            {
                var userRepository = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
                return RepositoryHandler.DoWithLock(userRepository, r =>
                {
                    var sandbox = SandboxHandler.Set.Where(x => x.LectureId == lecture.Id && x.Name == activity.Sandbox).Include(x => x.Lecture).ThenInclude(x => x.Owner).AsNoTracking().FirstOrDefault();
                    if(sandbox != null)
                    {
                        var command = $"cd ~/{activity.Directory}; {activity.Run}";
                        Executor.EnqueueExecution(user, sandbox,
                            $"Run activity {lecture.Owner.Account}/{lecture.Name}/{activity.Name}",
                            program: "/bin/bash",
                            sudo: false,
                            stdin: command,
                            stdoutCallback: stdoutCallback,
                            stderrCallback: stderrCallback,
                            cmdCallback: cmdCallback,
                            summaryCallback: summaryCallback,
                            doneCallback: doneCallback,
                            limit: new ResourceLimits
                            {
                                CpuTime = activity.Limits.CpuTime,
                                Memory = activity.Limits.Memory,
                                Pids = activity.Limits.Pids,
                                StdoutLength = activity.Limits.StdoutLength,
                                StderrLength = activity.Limits.StderrLength,
                            });
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
            }
        }

        public async Task<bool> SaveAndValidateActivityAsync(Data.Lecture lecture, Data.User user, Activity activity, string commitMessageBeforeValidate,
            IDictionary<string, string> textfiles, IDictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles,
            Func<Task> onSaveErrorOccurCallback = null,
            Func<IHubContext<ActivityHub>, bool, Task> doneCallback = null)
        {
            var result = !string.IsNullOrWhiteSpace(save(lecture, user, activity, commitMessageBeforeValidate, textfiles, binaryfiles, blocklyfiles));
            if (!result)
            {
                await onSaveErrorOccurCallback?.Invoke();
                return true;
            }
            else
            {
                var userRepository = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
                return RepositoryHandler.DoWithLock(userRepository, r =>
                {
                    var sandbox = SandboxHandler.Set.Where(x => x.LectureId == lecture.Id && x.Name == activity.Sandbox).Include(x => x.Lecture).ThenInclude(x => x.Owner).AsNoTracking().FirstOrDefault();
                    if (sandbox != null)
                    {
                        Queues.QueueBackgroundWorkItem(async token =>
                        {
                            var accept = await (activity.Validations.Child as IValidatable).ValidateAsync(async validation =>
                            {
                                try
                                {
                                    var stdout = new StringBuilder();
                                    var stderr = new StringBuilder();
                                    var command = $"cd ~/{activity.Directory}; {validation.Run}";
                                    await Executor.ExecuteAsync(user, sandbox, program: "/bin/bash", stdin: command, sudo: false,
                                    stdoutCallback: async (_, data) =>
                                    {
                                        stdout.AppendLine(data);
                                    },
                                    stderrCallback: async (_, data) =>
                                    {
                                        stderr.AppendLine(data);
                                    },
                                    limit: new ResourceLimits
                                    {
                                        CpuTime = activity.Limits.CpuTime,
                                        Memory = activity.Limits.Memory,
                                        Pids = activity.Limits.Pids,
                                        StdoutLength = activity.Limits.StdoutLength,
                                        StderrLength = activity.Limits.StderrLength,
                                    });
                                    if (validation.Type.ToLower() == "equals")
                                    {
                                        return stdout.ToString().Trim() == validation.Answer.Trim() && string.IsNullOrWhiteSpace(stderr.ToString().Trim());
                                    }
                                    return false;
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            });

                            using (var scope = ServiceScopeFactory.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetService<IHubContext<ActivityHub>>();
                                await doneCallback(context, accept);
                            }

                        }, user, $"Validate activity {lecture.Owner.Account}/{lecture.Name}/{activity.Name}", user.IsTeacher);



                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
            }
        }

        private string save(Data.Lecture lecture, Data.User user, Activity activity, string commitMessage,
            IDictionary<string, string> textfiles, IDictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles)
        {
            var userRepository = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
            return RepositoryHandler.DoWithLock(userRepository, r =>
            {
                try
                {
                    RepositoryHandler.CreateInitialLectureUserDataRepositoryIfNotExist(r, "master", lecture, user, "Initial Commit.");

                    var allowedFiles = activity.Files.Children.Select(x => x.Name);

                    var xs = new Dictionary<string, string>();
                    foreach (var key in textfiles.Keys.Where(x => allowedFiles.Contains(x)))
                    {
                        var path = $"home/{activity.ToPath(key)}";
                        xs.Add(path, textfiles[key]);
                    }
                    var ys = new Dictionary<string, byte[]>();
                    foreach (var key in binaryfiles.Keys.Where(x => allowedFiles.Contains(x)))
                    {
                        var path = $"home/{activity.ToPath(key)}";
                        var str = binaryfiles[key].Split(",", 2);
                        ys.Add(path, Convert.FromBase64String(str[1]));
                    }
                    var zs = new Dictionary<string, string>();
                    foreach (var key in blocklyfiles.Keys.Where(x => allowedFiles.Contains(x)))
                    {
                        var path = $"home/{activity.ToPath(key)}";
                        var json = blocklyfiles[key];
                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        var codeFilename = data["code-filename"];
                        xs.Add($"home/{activity.ToPath(key)}", data["xml"]);
                        xs.Add($"home/{activity.ToPath(codeFilename)}", data["code-body"]);
                    }

                    RepositoryHandler.SaveAndSync(r, "master", xs, ys, commitMessage, user.DisplayName, user.Email, user.Uid + 1000);
                    return RepositoryHandler.GetHashOfLatestCommit(r, "", "master");
                }
                catch (Exception e)
                {
                    return null;
                }
            });
        }

    }
}
