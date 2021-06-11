using Hugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hugin.Services
{

    public class RazorContentsBuildService : IContentsBuildService
    {
        class Dummy { }

        private readonly RazorLight.RazorLightEngine engine;
        private readonly RepositoryHandleService repositoryHandler;


        public RazorContentsBuildService(RepositoryHandleService _repositoryHandler)
        {
            repositoryHandler = _repositoryHandler;

            engine = new RazorLight.RazorLightEngineBuilder()
               .UseEmbeddedResourcesProject(typeof(Dummy))
               .UseMemoryCachingProvider()
               .DisableEncoding()
               .Build();
        }

        public async Task<string> BuildActivityAsync(LectureHandleService lectureHandler, Models.ActivityProfile prof, Data.User user, Data.Lecture lecture)
        {
            var repository = repositoryHandler.GetLectureContentsRepository(lecture.Owner.Account, lecture.Name);
            var commitInfo = repositoryHandler.ReadCommitInfo(repository, $"activities/{prof.ActivityRef}", prof.Rivision);

            dynamic viewbag = new System.Dynamic.ExpandoObject();
            var x = viewbag as IDictionary<string, Object>;
            foreach (var p in lectureHandler.GetLectureParameters(lecture, prof.Rivision))
            {
                x.Add(p.Key, p.Value.GetValue());
            }
            foreach (var p in prof.Parameters)
            {
                x.Add(p.Key, p.Value);
            }
            var model = new PageModel(repositoryHandler, lectureHandler, repository, user, lecture, prof.Rivision, prof.PagePath, commitInfo, viewbag);

            var page_hash = repositoryHandler.GetHashOfLatestCommit(repository, $"pages/{prof.PagePath}", prof.Rivision);
            var activity_hash = repositoryHandler.GetHashOfLatestCommit(repository, $"activities/{prof.ActivityRef}", prof.Rivision);
            var key = $"{lecture.Owner.Account}/{lecture.Name}/{prof.PagePath}:{page_hash}/{prof.Number}:{activity_hash}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            //sb.AppendLine("@{ DisableEncoding = true; }");
            sb.Append(repositoryHandler.ReadTextFile(repository, $"activities/{prof.ActivityRef}", prof.Rivision));

            return await engine.CompileRenderStringAsync(key, sb.ToString(), model, viewbag);

        }

        public async Task<string> BuildPageAsync(LectureHandleService lectureHandler, Data.User user, Data.Lecture lecture, string rivision, string pagePath)
        {
            var repository = repositoryHandler.GetLectureContentsRepository(lecture);
            var commitInfo = repositoryHandler.ReadCommitInfo(repository, $"pages/{pagePath}", rivision);

            dynamic viewbag = new System.Dynamic.ExpandoObject();
            var x = viewbag as IDictionary<string, Object>;
            foreach (var p in lectureHandler.GetLectureParameters(lecture, rivision))
            {
                x.Add(p.Key, p.Value.GetValue());
            }
            var model = new PageModel(repositoryHandler, lectureHandler, repository, user, lecture, rivision, pagePath, commitInfo, viewbag);


            var hash = repositoryHandler.GetHashOfLatestCommit(repository, $"pages/{pagePath}", rivision);
            var key = $"{lecture.Owner.Account}/{lecture.Name}/{pagePath}:{rivision}:{hash}";
            var sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            //sb.AppendLine("@{ DisableEncoding = true; }");
            sb.Append(repositoryHandler.ReadTextFile(repository, $"pages/{pagePath}", rivision));

            return await engine.CompileRenderStringAsync(key, sb.ToString(), model, viewbag);
        }
    }





    public class PageModel
    {
        private readonly Services.RepositoryHandleService RepositoryHandler;
        private readonly Services.LectureHandleService LectureHandler;
        private Data.Lecture lecture { get; }
        private Models.Repository repository { get; }

        public LectureModel Lecture { get; }
        public UserModel User { get; }
        public IEnumerable<UserModel> Users { get; }
        public IEnumerable<UserModel> Staffs => Users.Where(x => x.Role == RoleModel.Staff);
        public IEnumerable<UserModel> Students => Users.Where(x => x.Role == RoleModel.Studnet);
        public Models.CommitInfo CommitInfo { get; }

        public string PagePath { get; }
        public string Rivision { get; }
        public System.Dynamic.ExpandoObject ViewBag { get; }
        public PageModel(Services.RepositoryHandleService repositoryHandler, Services.LectureHandleService lectureHandler, Models.Repository repository, Data.User user, Data.Lecture lecture, string rivision, string page_path, Models.CommitInfo commitInfo, System.Dynamic.ExpandoObject viewBag)
        {
            RepositoryHandler = repositoryHandler;
            LectureHandler = lectureHandler;
            this.lecture = lecture;
            this.repository = repository;
            Lecture = new LectureModel(lecture);
            CommitInfo = commitInfo;
            PagePath = page_path;
            Rivision = rivision;
            ViewBag = viewBag;
            Users = LectureHandler.GetUserAndRoles(lecture).Select(x => new UserModel(x.Item1, x.Item2 == Data.LectureUserRelationship.LectureRole.Student ? RoleModel.Studnet : RoleModel.Staff));
            User = Users.Where(x => x.Account == user.Account).FirstOrDefault();
        }


        public string DateTimeToString(DateTime dt)
        {
            return Utility.DateTimeToString(dt);
        }
        public bool HasParameter(string parameterName)
        {
            return ((IDictionary<string, object>)ViewBag).ContainsKey(parameterName);
        }
        public string GetParameterAsString(string parameterName)
        {
            var x = ((IDictionary<string, object>)ViewBag);
            if (x.ContainsKey(parameterName))
            {
                var value = x[parameterName];
                if (value is DateTime dt)
                {
                    return Utility.DateTimeToString(dt);
                }
                else
                {
                    return value.ToString();
                }
            }
            else
            {
                return null;
            }
        }
        public object GetParameter(string parameterName)
        {
            var x = ((IDictionary<string, object>)ViewBag);
            return x.ContainsKey(parameterName) ? x[parameterName] : null;
        }
        public string EmbedTextFile(string path)
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

    //-----

    public class Utility
    {
        public static string DateTimeToString(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
    }

    public class LectureModel
    {
        public string Name { get; }
        public UserModel Owner { get; }
        public string Subject { get; }
        public string Description { get; }
        public LectureModel(Data.Lecture lecture)
        {
            Name = lecture.Name;
            Subject = lecture.Subject;
            Description = lecture.Description;
            Owner = new UserModel(lecture.Owner, RoleModel.Staff);
        }
    }

    public class UserModel
    {
        public string Account { get; }
        public string DisplayName { get; }
        public string EnglishName { get; }
        public string EmailAddress { get; }
        public RoleModel Role { get; }
        public UserModel(Data.User user, RoleModel role)
        {
            Account = user.Account;
            DisplayName = user.DisplayName;
            EnglishName = user.EnglishName;
            EmailAddress = user.Email;
            Role = role;
        }
    }

    public enum RoleModel { Staff, Studnet }
}
