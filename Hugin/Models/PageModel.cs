using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace Hugin.Models
{
    public class PageModel
    {
        private readonly Services.RepositoryHandleService RepositoryHandler;
        private readonly Services.LectureHandleService LectureHandler;
        private Data.Lecture lecture { get; }
        private Repository repository { get; }

        public LectureModel Lecture { get; }
        public UserModel User { get; }
        public IEnumerable<UserModel> Staffs { get; }
        public IEnumerable<UserModel> Students { get; }
        public CommitInfo CommitInfo { get; }

        public string PagePath { get; }
        public string Rivision { get; }
        public System.Dynamic.ExpandoObject ViewBag { get; }
        public PageModel(Services.RepositoryHandleService repositoryHandler, Services.LectureHandleService lectureHandler, Repository repository, Data.User user, Data.Lecture lecture, string rivision, string page_path, CommitInfo commitInfo, System.Dynamic.ExpandoObject viewBag)
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
            User = new UserModel(user, LectureHandler.GetStaffs(lecture).Any(x => x.Account == user.Account) ? RoleModel.Staff : RoleModel.Studnet);
            Staffs = lectureHandler.GetStaffs(lecture).Select(x => new UserModel(x, RoleModel.Staff));
            Students = lectureHandler.GetStudents(lecture).Select(x => new UserModel(x, RoleModel.Studnet));
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
            if(x.ContainsKey(parameterName))
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
                return RepositoryHandler.ReadTextFile( repository, path, Rivision);
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
