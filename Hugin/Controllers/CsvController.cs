using Hugin.Data;
using Hugin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hugin.Controllers
{
    [Authorize]
    public class CsvController : Controller
    {
        private readonly RepositoryHandleService RepositoryHandler;
        private readonly PermissionProviderService PermissionProvider;
        private readonly LectureHandleService LectureHandler;
        private readonly UserHandleService UserHandler;
        private readonly SubmissionHandleService SubmissionHandler;
        public CsvController(RepositoryHandleService repositoryHandler, PermissionProviderService permissionProvider, LectureHandleService lectureHandler, UserHandleService userHandler, SubmissionHandleService submissionHandler)
        {
            RepositoryHandler = repositoryHandler;
            PermissionProvider = permissionProvider;
            LectureHandler = lectureHandler;
            UserHandler = userHandler;
            SubmissionHandler = submissionHandler;
        }


        [HttpGet("Csv/LectureScores/{owner_account}/{lecture_name}/{branch}/{file_name}")]
        public IActionResult GradeCsv(string owner_account, string lecture_name, string branch, string file_name)
        {
            if (branch != "master")
            {
                return new UnauthorizedResult();
            }

            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            
            var repository = RepositoryHandler.GetLectureContentsRepository(lecture);
            if (!RepositoryHandler.IsInitialized(repository)) return new NotFoundResult();

            var loginUser = UserHandler.Set.Include(x => x.LectureUserRelationships)
                .Where(x => x.Account == User.Identity.Name).AsNoTracking().FirstOrDefault();
            if (loginUser == null) return new UnauthorizedResult();

            if(!PermissionProvider.CanShowSubmission(lecture, loginUser)) return new UnauthorizedResult();


            var submissionNames = SubmissionHandler.Set.Where(x => x.LectureId == lecture.Id).Select(x => x.ActivityName).Distinct().OrderBy(x => x).AsNoTracking().ToList();
            using (var ms = new MemoryStream())
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (var w = new StreamWriter(ms, Encoding.GetEncoding("Shift_JIS")))
                {

                    w.Write("Account, Name, English Name");
                    foreach (var name in submissionNames)
                    {
                        w.Write($", {name}");
                    }
                    w.WriteLine();

                    foreach (var user in LectureHandler.GetUsers(lecture).OrderBy(x => x.Account).AsNoTracking().ToList())
                    {
                        w.Write($"{user.Account}, {user.DisplayName}, {user.EnglishName}");
                        foreach (var name in submissionNames)
                        {
                            var s = SubmissionHandler.Set.Where(x => x.UserId == user.Id && x.LectureId == lecture.Id && x.ActivityName == name).OrderByDescending(x => x.Count).AsNoTracking().FirstOrDefault();
                            if (s != null)
                            {
                                w.Write($", {s.Grade}");
                            }
                            else
                            {
                                w.Write(", ");
                            }
                        }
                        w.WriteLine();
                    }
                }
                return File(ms.ToArray(), "text/csv");
            }
        }
    }
}
