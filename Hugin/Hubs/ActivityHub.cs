using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Hugin.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Hugin.Hubs
{


    [Authorize]
    public class ActivityHub : Hub
    {
        private readonly PermissionProviderService PermissionProvider;
        private readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly ActivityHandleService ActivityHandler;
        private readonly RepositoryHandleService RepositoryHandler;
        public readonly SubmissionNotifierService SubmissionNotifier;
        public readonly ActivityActionStatusService ActivityActionStatus;
        public readonly OnlineStatusService OnlineStatus;
        private readonly SubmissionHandleService SubmissionHandler;
        private readonly IHtmlLocalizer<Hugin.Lang> Localizer;
        public ActivityHub(IHtmlLocalizer<Hugin.Lang> localizer, PermissionProviderService permissionProvider, IServiceScopeFactory serviceScopeFactory, ActivityHandleService activityHandler, SubmissionHandleService submissionHandler, RepositoryHandleService repositoryHandler, SubmissionNotifierService submissionNotifier, ActivityActionStatusService activityActionStatus, OnlineStatusService onlineStatus)
        {
            Localizer = localizer;
            PermissionProvider = permissionProvider;
            ActivityHandler = activityHandler;
            ServiceScopeFactory = serviceScopeFactory;
            SubmissionHandler = submissionHandler;
            RepositoryHandler = repositoryHandler;
            SubmissionNotifier = submissionNotifier;
            ActivityActionStatus = activityActionStatus;
            OnlineStatus = onlineStatus;
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            await OnlineStatus.LeaveContentPageAsync(Context.User.Identity.Name);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendAccess(string lecture_owner, string lecture_name, string lecture_subject, string page_name, string user_account)
        {
            await OnlineStatus.VisitContentPageAsync(lecture_owner, lecture_name, lecture_subject, page_name, user_account);
        }

        private async Task sendActivityStatus(string activityId, Lecture lecture, User user, Models.Activity activity)
        {
            var submission = SubmissionHandler.Set.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.State != Submission.SubmissionState.Deleted && x.LectureId == lecture.Id && x.ActivityName == activity.Name && x.UserId == user.Id)
                .OrderByDescending(x => x.Count).AsNoTracking().FirstOrDefault();

            var dict = new Dictionary<string, object>();
            if (submission != null)
            {
                dict.Add("submissionState", submission.State.ToString());
                if (submission.State == Submission.SubmissionState.AcceptingResubmit ||
                    submission.State == Submission.SubmissionState.RequiringResubmit ||
                    submission.State == Submission.SubmissionState.Confirmed ||
                    submission.State == Submission.SubmissionState.Disqualified)
                {
                    dict.Add("grade", submission.Grade);
                    dict.Add("feedbackComment", submission.FeedbackComment);
                    dict.Add("markedAt", submission.MarkedAt);
                }
                if (submission.State == Submission.SubmissionState.AcceptingResubmit ||
                    submission.State == Submission.SubmissionState.RequiringResubmit)
                {
                    dict.Add("deadline", submission.ResubmitDeadline);
                }
                else
                {
                    dict.Add("deadline", activity.Deadline);
                }
            }
            else
            {
                dict.Add("deadline", activity.Deadline);
            }
            var json = System.Text.Json.JsonSerializer.Serialize(dict);
            await Clients.Caller.SendAsync("ReceiveActivityStatus", activityId, json);
        }

        public async Task SendActivityStatus(string activityId, string activityProfile)
        {
            var (_, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            if(PermissionProvider.CanShowActivity(lecture, user, activity))
            {
                await sendActivityStatus(activityId, lecture, user, activity);
            }

        }

        public async Task SendSaveRequest(string activityId, string activityProfile, Dictionary<string, string> textfiles, Dictionary<string, string> binaryfiles, Dictionary<string, string> blocklyfiles)
        {
            var requestedAt = DateTime.Now;

            var (profile, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            if (PermissionProvider.CanShowActivity(lecture, user, activity) && activity.UseSave())
            {
                var commitMessage = $"Save: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}";

                var result = !string.IsNullOrWhiteSpace(ActivityHandler.SaveActivity(lecture, user, activity, commitMessage, textfiles, binaryfiles, blocklyfiles));

                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    if (profile.Rivision == lecture.DefaultBranch)
                    {
                        var activityActionHandler = scope.ServiceProvider.GetService<ActivityActionHandleService>();
                        activityActionHandler.AddNew(new ActivityAction
                        {
                            ActivityName = activity.Name,
                            ActivityActionType = Models.ActivityActionTypes.Save,
                            Summary = result ? Localizer["Success"].Value : Localizer["Failed"].Value,
                            LectureId = lecture.Id,
                            UserId = user.Id,
                            RequestedAt = requestedAt,
                            Page = profile.PagePath,
                            Tags = activity.Tags
                        });
                        await ActivityActionStatus.Record(user.Account, new ActivityActionStatusService.Status
                        {
                            LectureOwner = lecture.Owner.Account,
                            LectureName = lecture.Name,
                            PageName = profile.PagePath,
                            ActivityName = activity.Name,
                            LectureSubject = lecture.Subject,
                            Summary = result ? "Success" : "Failure",
                            ActionType = Models.ActivityActionTypes.Save,
                            UpdatedAt = requestedAt
                        });
                    }
                }
                await Clients.Caller.SendAsync("ReceiveActionPermissions", activityId, activity.Flags.CanValidateBeforeRun, activity.Flags.CanSubmitBeforeAccept && activity.Flags.CanSubmitBeforeRun);
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, result ? Localizer["Success"].Value : null, !result ? Localizer["Failed"].Value : null, null);
            }
        }

        public async Task SendRunRequest(string activityId, string activityProfile, string runnerName, Dictionary<string, string> textfiles, Dictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles)
        {
            var requestedAt = DateTime.Now;

            var (profile, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            var runner = activity.Runners.Runners.Where(x => x.Name == runnerName).FirstOrDefault();

            if (PermissionProvider.CanShowActivity(lecture, user, activity) && runner != null)
            {
                var commitMessageBeforeRun = $"SaveForRun: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}";
                var connectionId = Context.ConnectionId;

                var userRepository = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
                var summary = new StringBuilder();

                var additionalFiles = new StringBuilder();

                var result = await ActivityHandler.SaveAndRunActivityAsync(lecture, user, activity, runner, commitMessageBeforeRun, textfiles, binaryfiles, blocklyfiles,
                    onSaveErrorOccurCallback: async () =>
                    {
                        await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["SaveError"].Value);
                    },
                    stdoutCallback: async (context, data) =>
                    {
                        await context.Clients.Client(connectionId).SendAsync("ReceiveStdout", activityId, data);
                    },
                    stderrCallback: async (context, data) =>
                    {
                        await context.Clients.Client(connectionId).SendAsync("ReceiveStderr", activityId, data);
                    },
                    cmdCallback: async (context, data) =>
                    {
                        if(System.Text.RegularExpressions.Regex.IsMatch(data, @"^submit\s"))
                        {
                            foreach(var x in data.Split(" ",  StringSplitOptions.RemoveEmptyEntries).Skip(1))
                            {
                                additionalFiles.AppendLine(activity.ToPath(x));
                            }
                        }
                        else
                        {
                            await context.Clients.Client(connectionId).SendAsync("ReceiveCommand", activityId, data);
                        }
                    },
                    summaryCallback: async (context, data) =>
                    {
                        await Task.Run(() =>
                        {
                            summary.AppendLine(data);
                        });
                    },
                    doneCallback: async (context, code) =>
                    {
                        var commitMessageAfterRun = $"Run: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}";
                        RepositoryHandler.CommitAll(userRepository, "master", $"Run: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}", user.DisplayName, user.Email);
                        RepositoryHandler.PushToBare(userRepository, "master");

                        if (profile.Rivision == lecture.DefaultBranch)
                        {
                            using (var scope = ServiceScopeFactory.CreateScope())
                            {
                                var activityActionHandler = scope.ServiceProvider.GetService<ActivityActionHandleService>();
                                activityActionHandler.AddNew(new ActivityAction
                                {
                                    ActivityName = activity.Name,
                                    ActivityActionType = Models.ActivityActionTypes.Run,
                                    Summary = summary.ToString(),
                                    LectureId = lecture.Id,
                                    UserId = user.Id,
                                    RequestedAt = requestedAt,
                                    Page = profile.PagePath,
                                    Tags = activity.Tags,
                                    AdditionalFiles = additionalFiles.ToString()
                                });
                                await ActivityActionStatus.Record(user.Account, new ActivityActionStatusService.Status
                                {
                                    LectureOwner = lecture.Owner.Account,
                                    LectureName = lecture.Name,
                                    PageName = profile.PagePath,
                                    ActivityName = activity.Name,
                                    LectureSubject = lecture.Subject,
                                    Summary = summary.ToString(),
                                    ActionType = Models.ActivityActionTypes.Run,
                                    UpdatedAt = requestedAt
                                });
                            }
                        }
                        await context.Clients.Client(connectionId).SendAsync("ReceiveActionPermissions", activityId, activity.Flags.CanValidateBeforeRun || !runner.Auxiliary, activity.Flags.CanSubmitBeforeAccept);
                        await context.Clients.Client(connectionId).SendAsync("ReceiveActionResult", activityId, null, null, null);
                    });
                if (!result)
                {
                    await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, string.Format(Localizer["ThSandboxDoesNotExist"].Value, activity.Sandbox), null);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }

        public async Task SendValidateRequest(string activityId, string activityProfile, Dictionary<string, string> textfiles, Dictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles)
        {
            var requestedAt = DateTime.Now;

            var (profile, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            if (PermissionProvider.CanShowActivity(lecture, user, activity) && activity.UseValidate())
            {
                var commitMessageBeforeValidate = $"SaveForValidate: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}";
                var connectionId = Context.ConnectionId;

                var userRepository = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
                var summary = new StringBuilder();
                var lazies = new List<string>();

                var result = await ActivityHandler.SaveAndValidateActivityAsync(lecture, user, activity, commitMessageBeforeValidate, textfiles, binaryfiles, blocklyfiles,
                    onSaveErrorOccurCallback: async () =>
                    {
                        await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["SaveError"].Value);
                    },
                    doneCallback: async (context, result) =>
                    {
                        var t = result ? Localizer["Accept"].Value : Localizer["Reject"].Value;
                        var commitMessageAfterRun = $"Validate{t}: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}";
                        RepositoryHandler.CommitAll(userRepository, "master", $"Validate{t}: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}", user.DisplayName, user.Email);
                        RepositoryHandler.PushToBare(userRepository, "master");

                        using (var scope = ServiceScopeFactory.CreateScope())
                        {
                            var activityActionHandler = scope.ServiceProvider.GetService<ActivityActionHandleService>();
                            activityActionHandler.AddNew(new ActivityAction
                            {
                                ActivityName = activity.Name,
                                ActivityActionType = result ? Models.ActivityActionTypes.ValidationAccept : Models.ActivityActionTypes.ValidationReject,
                                Summary = t,
                                LectureId = lecture.Id,
                                UserId = user.Id,
                                RequestedAt = requestedAt,
                                Page = profile.PagePath,
                                Tags = activity.Tags
                            });
                            await ActivityActionStatus.Record(user.Account, new ActivityActionStatusService.Status
                            {
                                LectureOwner = lecture.Owner.Account,
                                LectureName = lecture.Name,
                                PageName = profile.PagePath,
                                ActivityName = activity.Name,
                                LectureSubject = lecture.Subject,
                                Summary = t,
                                ActionType = result ? Models.ActivityActionTypes.ValidationAccept : Models.ActivityActionTypes.ValidationReject,
                                UpdatedAt = requestedAt
                            });
                        }
                        if(result)
                        {
                            await context.Clients.Client(connectionId).SendAsync("ReceiveStdout", activityId, Localizer["Accept"].Value);
                            await context.Clients.Client(connectionId).SendAsync("ReceiveActionPermissions", activityId, null, true);
                            await context.Clients.Client(connectionId).SendAsync("ReceiveActionResult", activityId, null, null, null);
                        }
                        else
                        {
                            await context.Clients.Client(connectionId).SendAsync("ReceiveStderr", activityId, Localizer["Reject"].Value);
                            await context.Clients.Client(connectionId).SendAsync("ReceiveActionPermissions", activityId, null, activity.Flags.CanSubmitBeforeAccept || false);
                            await context.Clients.Client(connectionId).SendAsync("ReceiveActionResult", activityId, null, null, null);
                        }
                    });
                if (!result)
                {
                    await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, string.Format(Localizer["ThSandboxDoesNotExist"].Value, activity.Sandbox), null);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }



        public async Task SendSubmitRequest(string activityId, string activityProfile, Dictionary<string, string> textfiles, Dictionary<string, string> binaryfiles, IDictionary<string, string> blocklyfiles, string submitMessage)
        {
            var requestedAt = DateTime.Now;

            var (profile, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            if (PermissionProvider.CanSubmitActivity(lecture, user, activity) && activity.UseSubmit())
            {

                var latest = SubmissionHandler.Set.Where(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.ActivityName == activity.Name)
                    .OrderByDescending(x => x.SubmittedAt).FirstOrDefault();
                
                if(latest != null && latest.State == Submission.SubmissionState.Confirmed)
                {
                    await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["TheActivityHasBeenConfirmed"].Value, null);
                    return;
                }
                if (latest != null && latest.State == Submission.SubmissionState.Disqualified)
                {
                    await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["TheActivityHasBeenDisqualified"].Value, null);
                    return;
                }

                if (!activity.Flags.CanSubmitAfterDeadline)
                {
                    if(latest != null)
                    {
                        if (latest.State == Submission.SubmissionState.AcceptingResubmit || latest.State == Submission.SubmissionState.RequiringResubmit)
                        {
                            if (latest.ResubmitDeadline < requestedAt)
                            {
                                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["TheResubmitDeadlineHasBeenPassed"].Value, null);
                                return;
                            }
                        }
                        else
                        {
                            if(activity.Deadline < requestedAt)
                            {
                                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["TheDeadlineHasBeenPassed"].Value, null);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (activity.Deadline < requestedAt)
                        {
                            await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, Localizer["TheDeadlineHasBeenPassed"].Value, null);
                            return;
                        }
                    }
                }

                var commitMessage = new StringBuilder();
                commitMessage.AppendLine($"Submit: {lecture.Owner.Account}/{lecture.Name}/{activity.Name}");
                foreach (var fn in activity.GetSubmittedFilePaths())
                {
                    commitMessage.AppendLine(fn);
                }

                var hash = ActivityHandler.SaveActivity(lecture, user, activity, commitMessage.ToString(), textfiles, binaryfiles, blocklyfiles);
                var result = !string.IsNullOrWhiteSpace(hash);

                if (result && profile.Rivision == lecture.DefaultBranch)
                {
                    using (var scope = ServiceScopeFactory.CreateScope())
                    {
                        var activityActionHandler = scope.ServiceProvider.GetService<ActivityActionHandleService>();
                        activityActionHandler.AddNew(new ActivityAction
                        {
                            ActivityName = activity.Name,
                            ActivityActionType = Models.ActivityActionTypes.Submit,
                            Summary = result ? hash : Localizer["Failed"].Value,
                            LectureId = lecture.Id,
                            UserId = user.Id,
                            RequestedAt = requestedAt,
                            Page = profile.PagePath,
                            Tags = activity.Tags
                        });
                        await ActivityActionStatus.Record(user.Account, new ActivityActionStatusService.Status
                        {
                            LectureOwner = lecture.Owner.Account,
                            LectureName = lecture.Name,
                            PageName = profile.PagePath,
                            ActivityName = activity.Name,
                            LectureSubject = lecture.Subject,
                            Summary = result ? hash : Localizer["Failed"].Value,
                            ActionType = Models.ActivityActionTypes.Submit,
                            UpdatedAt = requestedAt
                        });

                        var submissionHandler = scope.ServiceProvider.GetService<SubmissionHandleService>();
                        var count = submissionHandler.Set.Where(x => x.LectureId == lecture.Id && x.ActivityName == activity.Name && x.UserId == user.Id).Count();

                        var deadline = (latest?.State == Submission.SubmissionState.RequiringResubmit || latest?.State == Submission.SubmissionState.AcceptingResubmit) ? latest.ResubmitDeadline : activity.Deadline;

                        DateTime rd = DateTime.Now.AddDays(7);
                        if(deadline.HasValue)
                        {
                            var d = DateTime.Now.AddDays(7);
                            rd = new DateTime(d.Year, d.Month, d.Day, deadline.Value.Hour, deadline.Value.Minute, deadline.Value.Second);
                        }

                        var files = string.Join(Environment.NewLine, activity.GetSubmittedFilePaths());
                        var latestRunActivity = activityActionHandler.Set.Where(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.ActivityName == activity.Name && x.ActivityActionType == Models.ActivityActionTypes.Run)
                                                    .OrderByDescending(x => x.RequestedAt).FirstOrDefault();
                        if(latestRunActivity != null && !string.IsNullOrWhiteSpace(latestRunActivity.AdditionalFiles))
                        {
                            files += Environment.NewLine;
                            files += latestRunActivity.AdditionalFiles;
                        }

                        var numOfSaves = activityActionHandler.Set.Where(x => x.ActivityName == activity.Name && x.LectureId == lecture.Id && x.UserId == user.Id && x.ActivityActionType == Models.ActivityActionTypes.Save).Count();
                        var numOfRuns = activityActionHandler.Set.Where(x => x.ActivityName == activity.Name && x.LectureId == lecture.Id && x.UserId == user.Id && x.ActivityActionType == Models.ActivityActionTypes.Run).Count();
                        var numOfValidateRejects = activityActionHandler.Set.Where(x => x.ActivityName == activity.Name && x.LectureId == lecture.Id && x.UserId == user.Id && x.ActivityActionType == Models.ActivityActionTypes.ValidationReject).Count();
                        var numOfValidateAccepts = activityActionHandler.Set.Where(x => x.ActivityName == activity.Name && x.LectureId == lecture.Id && x.UserId == user.Id && x.ActivityActionType == Models.ActivityActionTypes.ValidationAccept).Count();

                        submissionHandler.AddNew(new Submission
                        {
                            LectureId = lecture.Id,
                            ActivityName = activity.Name,
                            UserId = user.Id,
                            SubmittedAt = requestedAt,
                            Deadline = deadline,
                            SubumitComment = submitMessage,
                            Hash = hash,
                            SubmittedFiles = files,
                            State = activity.Flags.ConfirmAutomatically ? Submission.SubmissionState.Confirmed : Submission.SubmissionState.Submitted,
                            Grade = activity.Flags.ConfirmAutomatically ? Localizer["OK"].Value : null,
                            MarkedAt = activity.Flags.ConfirmAutomatically ? DateTime.Now : null,
                            MarkerUserId = activity.Flags.ConfirmAutomatically ? lecture.OwnerId : null,
                            FeedbackComment = activity.Flags.ConfirmAutomatically ? Localizer["ConfirmAutomatically"].Value : null,
                            Page = profile.PagePath,
                            Count = count + 1,
                            ResubmitDeadline = rd,
                            NumOfSaves = numOfSaves,
                            NumOfRuns = numOfRuns,
                            NumOfValidateRejects = numOfValidateRejects,
                            NumOfValidateAccepts = numOfValidateAccepts,
                            Tags = activity.Tags
                        });
                        await SubmissionNotifier.Update();
                    }
                }
                await sendActivityStatus(activityId, lecture, user, activity);
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, result ? Localizer["Success"].Value : null, !result ? Localizer["Failed"].Value : null, null);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }

        public async Task SendDiscardRequest(string activityId, string activityProfile)
        {
            var (_, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            await Clients.Caller.SendAsync("ReceiveActionPermissions", activityId, activity.Flags.CanValidateBeforeRun, activity.Flags.CanSubmitBeforeAccept && activity.Flags.CanSubmitBeforeRun);

            if (PermissionProvider.CanShowActivity(lecture, user, activity) && activity.UseDiscard())
            {
                var repository = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
                var dict = new Dictionary<string, string>();
                var initialized = RepositoryHandler.IsInitialized(repository);
                foreach (var x in activity.Files.Children.Where(x => !(x is Models.ActivityFilesUpload)))
                {
                    var path = $"home/{activity.Directory}/{x.Name}";
                    if (initialized && RepositoryHandler.Exists(repository, path, "master"))
                    {
                        dict[x.Name] = RepositoryHandler.ReadTextFile(repository, path, "master");
                    }
                    else
                    {
                        dict[x.Name] = x.HasDefault() ? x.Default : null;
                    }
                }
                var json = System.Text.Json.JsonSerializer.Serialize(dict);
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, json);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }
        public async Task SendResetRequest(string activityId, string activityProfile)
        {
            var (_, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            await Clients.Caller.SendAsync("ReceiveActionPermissions", activityId, activity.Flags.CanValidateBeforeRun, activity.Flags.CanSubmitBeforeAccept && activity.Flags.CanSubmitBeforeRun);
            if (PermissionProvider.CanShowActivity(lecture, user, activity) && activity.UseReset())
            {
                var dict = new Dictionary<string, string>();
                foreach (var x in activity.Files.Children)
                {
                    if (x.HasDefault())
                    {
                        dict[x.Name] = x.Default;
                    }
                    else
                    {
                        dict[x.Name] = null;
                    }
                }
                var json = System.Text.Json.JsonSerializer.Serialize(dict);
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, json);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }

        public async Task SendAnswerRequest(string activityId, string activityProfile)
        {
            var (_, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            await Clients.Caller.SendAsync("ReceiveActionPermissions", activityId, activity.Flags.CanValidateBeforeRun, activity.Flags.CanSubmitBeforeAccept && activity.Flags.CanSubmitBeforeRun);
            if (PermissionProvider.CanAnswerActivity(lecture, user, activity) && activity.UseAnswer())
            {
                var dict = new Dictionary<string, string>();
                foreach (var x in activity.Files.Children.Where(x => x.HasAnswer()))
                {
                    dict[x.Name] = x.Answer;
                }
                var json = System.Text.Json.JsonSerializer.Serialize(dict);
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, json);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }
        public async Task SendPullRequest(string activityId, string userAccount, string activityProfile)
        {
            var (_, lecture, user, activity, _) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            await Clients.Caller.SendAsync("ReceiveActionPermissions", activityId, activity.Flags.CanValidateBeforeRun, activity.Flags.CanSubmitBeforeAccept && activity.Flags.CanSubmitBeforeRun);
            if (PermissionProvider.CanMarkSubmission(lecture, user))
            {
                var repository = RepositoryHandler.GetLectureUserDataRepository(lecture, userAccount);
                var dict = new Dictionary<string, string>();
                var initialized = RepositoryHandler.IsInitialized(repository);
                foreach (var x in activity.Files.Children.Where(x => !(x is Models.ActivityFilesUpload)))
                {
                    var path = $"home/{activity.Directory}/{x.Name}";
                    if (initialized && RepositoryHandler.Exists(repository, path, "master"))
                    {
                        dict[x.Name] = RepositoryHandler.ReadTextFile(repository, path, "master");
                    }
                    else
                    {
                        dict[x.Name] = x.HasDefault() ? x.Default : null;
                    }
                }
                var json = System.Text.Json.JsonSerializer.Serialize(dict);
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, json);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
            }
        }
        public async Task SendActivityXmlRequest(string activityId, string activityProfile)
        {
            var (_, _, _, _, xml) = await ActivityHandler.DecryptProfileAsync(activityProfile);

            await Clients.Caller.SendAsync("ReceiveActivityXmlResult", activityId, xml);
            await Clients.Caller.SendAsync("ReceiveActionResult", activityId, null, null, null);
        }
    }
}
