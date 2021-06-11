using Microsoft.Extensions.Caching.Memory;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hugin.Services
{


    public class ScribanContentsBuildService : IContentsBuildService
    {
        private readonly ApplicationConfigurationService config;
        private readonly RepositoryHandleService repositoryHandler;
        private readonly IMemoryCache cache;

        public ScribanContentsBuildService(ApplicationConfigurationService _config, RepositoryHandleService _repositoryHandler, IMemoryCache _cache)
        {
            config = _config;
            repositoryHandler = _repositoryHandler;
            cache = _cache;
        }

        public async Task<string> BuildActivityAsync(LectureHandleService lectureHandler, Models.ActivityProfile prof, Data.User user, Data.Lecture lecture)
        {
            var repository = repositoryHandler.GetLectureContentsRepository(lecture);
            var commitInfo = repositoryHandler.ReadCommitInfo(repository, $"activities/{prof.ActivityRef}", prof.Rivision);
            var raw = repositoryHandler.ReadTextFile(repository, $"activities/{prof.ActivityRef}", prof.Rivision);
            var sb = new StringBuilder();
            sb.AppendLine("{{ date.format = '%Y-%m-%dT%H:%M:%S' }}");
            sb.Append(raw);

            var page_hash = repositoryHandler.GetHashOfLatestCommit(repository, $"pages/{prof.PagePath}", prof.Rivision);
            var activity_hash = repositoryHandler.GetHashOfLatestCommit(repository, $"activities/{prof.ActivityRef}", prof.Rivision);
            var key = $"{lecture.Owner.Account}/{lecture.Name}/{prof.PagePath}:{page_hash}/{prof.Number}:{activity_hash}";
            var template = cache.GetOrCreate(key, x => Scriban.Template.Parse(sb.ToString()));

            var context = buildContext();
            var model = buildModel(lectureHandler, lecture, prof.Rivision, prof);
            var huginObject = new HuginScriptObject(repositoryHandler, lectureHandler, repository, user, lecture, prof.Rivision, prof.Rivision, commitInfo, model);
            model.Import(huginObject);
            context.PushGlobal(model);

            return tempalteRender(new TimeSpan(0, 0, config.GetRenderTimeLimit()), 1000000, context, template);
        }

        public async Task<string> BuildPageAsync(LectureHandleService lectureHandler, Data.User user, Data.Lecture lecture, string rivision, string pagePath)
        {
            var repository = repositoryHandler.GetLectureContentsRepository(lecture);
            var commitInfo = repositoryHandler.ReadCommitInfo(repository, $"pages/{pagePath}", rivision);
            var raw = repositoryHandler.ReadTextFile(repository, $"pages/{pagePath}", rivision);
            var sb = new StringBuilder();
            sb.AppendLine("{{ date.format = '%Y-%m-%dT%H:%M:%S' }}");
            sb.Append(raw);

            var hash = repositoryHandler.GetHashOfLatestCommit(repository, $"pages/{pagePath}", rivision);
            var key = $"{lecture.Owner.Account}/{lecture.Name}/{pagePath}:{rivision}:{hash}";
            var template = cache.GetOrCreate(key, x => Scriban.Template.Parse(sb.ToString()));

            var context = buildContext();


            var model = buildModel(lectureHandler, lecture, rivision);
            var huginObject = new HuginScriptObject(repositoryHandler, lectureHandler, repository, user, lecture, rivision, pagePath, commitInfo, model);
            model.Import(huginObject);
            context.PushGlobal(model);

            return tempalteRender(new TimeSpan(0, 0, config.GetRenderTimeLimit()), 1000000, context, template);
        }

        private Scriban.TemplateContext buildContext()
        {
            var context = new Scriban.TemplateContext();
            context.LoopLimit = 500;
            context.RecursiveLimit = 5;
            context.StrictVariables = false;
            context.RegexTimeOut = new TimeSpan(0, 0, config.GetRenderTimeLimit());
            return context;
        }

        private string tempalteRender(TimeSpan timeSpan, int maxLength, Scriban.TemplateContext context, Scriban.Template template)
        {
            var task = Task.Run(async () => await template.RenderAsync(context));
            if (task.Wait(timeSpan))
            {
                var text = task.Result;
                return text.Substring(0, Math.Min(maxLength, text.Length));
            }
            else
            {
                throw new Exception("Render time out");
            }
        }

        private Scriban.Runtime.ScriptObject buildModel(LectureHandleService lectureHandler, Data.Lecture lecture, string rivision, Models.ActivityProfile prof = null)
        {
            var model = new ScriptObject();
            foreach (var p in lectureHandler.GetLectureParameters(lecture, rivision))
            {
                model.Add(p.Key, p.Value.GetValue());
            }
            if (prof != null)
            {
                foreach (var p in prof.Parameters)
                {
                    if(model.ContainsKey(p.Key))
                    {
                        model[p.Key] = p.Value;
                    }
                    else
                    {
                        model.Add(p.Key, p.Value);
                    }
                }
            }
            return model;
        }

        public class HuginScriptObject
        {
            private readonly Services.RepositoryHandleService RepositoryHandler;
            private readonly Services.LectureHandleService LectureHandler;
            private Data.Lecture lecture { get; }
            private Models.Repository repository { get; }


            public Lecture Lecture { get; }
            public User User { get; }
            public IEnumerable<User> Users { get; }
            public IEnumerable<User> Staffs => Users.Where(x => x.Role == Role.Staff);
            public IEnumerable<User> Students => Users.Where(x => x.Role == Role.Studnet);
            public Models.CommitInfo CommitInfo { get; }
            public string PagePath { get; }
            public string Rivision { get; }
            public string Root { get; }
            public ScriptObject Model { get; }

            public HuginScriptObject(RepositoryHandleService repositoryHandler, LectureHandleService lectureHandler, Models.Repository repository, Data.User user, Data.Lecture lecture, string rivision, string page_path, Models.CommitInfo commitInfo, ScriptObject model)
            {
                RepositoryHandler = repositoryHandler;
                LectureHandler = lectureHandler;
                this.lecture = lecture;
                this.repository = repository;
                Lecture = new Lecture(lecture);
                CommitInfo = commitInfo;
                PagePath = page_path;
                Rivision = rivision;
                Root = $"/Page/{lecture.Owner.Account}/{lecture.Name}/{lecture.DefaultBranch}";
                Model = model;
                Users = LectureHandler.GetUserAndRoles(lecture).Select(x => new User(x.Item1, x.Item2 == Data.LectureUserRelationship.LectureRole.Student ? Role.Studnet : Role.Staff));
                User = Users.Where(x => x.Account == user.Account).FirstOrDefault();
                addFuncs();
            }

            private void addFuncs()
            {
                Model.Import("is_null_or_whitespace", new Func<string, bool>(string.IsNullOrWhiteSpace));
                Model.Import("encode_html", new Func<string, string>(EncodeHtml));
                Model.Import("decode_html", new Func<string, string>(DecodeHtml));
                Model.Import("get_parameter", new Func<string, object>(GetParameter));
                Model.Import("has_parameter", new Func<string, bool>(HasParameter));
                Model.Import("date_time_to_string", new Func<DateTime, string>(DateTimeToString));
                Model.Import("embed_text_file", new Func<string, string>(EmbedTextFile));
            }
            private object GetParameter(string parameterName)
            {
                var x = ((IDictionary<string, object>)Model);
                return x.ContainsKey(parameterName) ? x[parameterName] : null;
            }
            private bool HasParameter(string parameterName)
            {
                return ((IDictionary<string, object>)Model).ContainsKey(parameterName);
            }
            private string DecodeHtml(string encoded_html)
            {
                return encoded_html?.Replace("&lt;", "<")?.Replace("&gt;", ">")?.Replace("&quot;", "\"")?.Replace("&amp;", "&");
            }
            private string EncodeHtml(string raw_html)
            {
                return raw_html?.Replace("<", "&lt;")?.Replace(">", "&gt;")?.Replace("\"", "&quot;")?.Replace("&", "&amp;");
            }

            private string DateTimeToString(DateTime dt)
            {
                return Utility.DateTimeToString(dt);
            }
            private string EmbedTextFile(string path)
            {
                try
                {
                    return RepositoryHandler.ReadTextFile(repository, path, Rivision);
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }



        public class Lecture
        {
            public string Name { get; }
            public User Owner { get; }
            public string Subject { get; }
            public string Description { get; }
            public Lecture(Data.Lecture lecture)
            {
                Name = lecture.Name;
                Subject = lecture.Subject;
                Description = lecture.Description;
                Owner = new User(lecture.Owner, Role.Staff);
            }
        }
        public class User
        {
            public string Account { get; }
            public string DisplayName { get; }
            public string EnglishName { get; }
            public string EmailAddress { get; }
            public Role Role { get; }
            public User(Data.User user, Role role)
            {
                Account = user.Account;
                DisplayName = user.DisplayName;
                EnglishName = user.EnglishName;
                EmailAddress = user.Email;
                Role = role;
            }
        }
        public enum Role { Staff, Studnet }
    }
}
