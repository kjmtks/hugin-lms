using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;
using Hugin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using static Hugin.Data.LectureUserRelationship;

namespace Hugin.Controllers
{

    [Authorize]
    public class RawFileController : Controller
    {
        PermissionProviderService PermissionProvider;
        DatabaseContext DatabaseContext;
        RepositoryHandleService RepositoryHandler;
        public RawFileController(PermissionProviderService permissionProvider, DatabaseContext databaseContext, RepositoryHandleService repositoryHandler)
        {
            PermissionProvider = permissionProvider;
            DatabaseContext = databaseContext;
            RepositoryHandler = repositoryHandler;
        }

        [HttpGet("/RawFile/SystemFile/{**path}")]
        public IActionResult SystemFile(string path)
        {
            var user = DatabaseContext.Users.Where(x => x.Account == HttpContext.User.Identity.Name && x.IsAdmin).FirstOrDefault();
            if (PermissionProvider.CanShowSystemRawFile(user))
            {
                try
                {
                    var ms = new MemoryStream();
                    using (var r = new FileStream($"/{path}", FileMode.Open, FileAccess.Read))
                    {
                        r.CopyTo(ms);
                    }

                    var contentType = Models.ContentTypeProvider.GetContentType(path);
                    ms.Seek(0, SeekOrigin.Begin);
                    return File(ms, contentType);
                }
                catch
                {
                    return new NotFoundResult();
                }
            }
            else
            {
                return new UnauthorizedResult();
            }
        }
        [HttpGet("/RawFile/LectureContents/{lectureOwner}/{lectureName}/{rivision}/{**path}")]
        public IActionResult LectureContents(string lectureOwner, string lectureName, string rivision, string path)
        {
            var loginUser = DatabaseContext.Users.Where(x => x.Account == HttpContext.User.Identity.Name).FirstOrDefault();
            if (loginUser == null)
            {
                return new UnauthorizedResult();
            }

            var lecture = DatabaseContext.Lectures.Include(x => x.Owner).Where(x => x.Owner.Account == lectureOwner && x.Name == lectureName).FirstOrDefault();

            if (PermissionProvider.CanReadLectureContentsRepository(lecture, loginUser))
            {
                try
                {
                    var repo = RepositoryHandler.GetLectureContentsRepository(lecture);
                    if (rivision == "master")
                    {
                        var filePath = $"{repo.GetNonBaredFullPath("master")}{path}";
                        if (System.IO.File.Exists(filePath))
                        {
                            var data = System.IO.File.ReadAllBytes(filePath);
                            var contentType = Models.ContentTypeProvider.GetContentType(filePath);
                            return File(data, contentType);
                        }
                        else
                        {
                            return new NotFoundResult();
                        }
                    }
                    else
                    {
                        if (!RepositoryHandler.IsInitialized(repo) || !RepositoryHandler.Exists(repo, path, rivision))
                        {
                            return new NotFoundResult();
                        }
                        var (data, istext) = RepositoryHandler.ReadFileWithTypeCheck(repo, path, rivision);
                        var contentType = Models.ContentTypeProvider.GetContentType(path, istext ? "text/plain; charset=utf-8" : "application/octet-stream");
                        return File(data, contentType);
                    }
                }
                catch
                {
                    return new NotFoundResult();
                }
            }
            else
            {
                return new NotFoundResult();
            }

        }

        [HttpGet("/RawFile/LectureUserData/{lectureOwner}/{lectureName}/{account}/{rivision}/{**path}")]
        public IActionResult LectureUserData(string lectureOwner, string lectureName, string account, string rivision, string path)
        {
            var loginUser = DatabaseContext.Users.Where(x => x.Account == HttpContext.User.Identity.Name).FirstOrDefault();
            if (loginUser == null)
            {
                return new UnauthorizedResult();
            }

            var user = DatabaseContext.Users.Where(x => x.Account == account).FirstOrDefault();
            var lecture = DatabaseContext.Lectures.Include(x => x.Owner).Where(x => x.Owner.Account == lectureOwner && x.Name == lectureName).FirstOrDefault();

            if(PermissionProvider.CanShowLectureUserDataRawFile(lecture, user, loginUser))
            {
                try
                {
                    var repo = RepositoryHandler.GetLectureUserDataRepository(lecture, user.Account);
                    if (rivision == "master")
                    {
                        var filePath = $"{repo.GetNonBaredFullPath("master")}{path}";
                        if(System.IO.File.Exists(filePath))
                        {
                            var data = System.IO.File.ReadAllBytes(filePath);
                            var contentType = Models.ContentTypeProvider.GetContentType(filePath);
                            return File(data, contentType);
                        }
                        else
                        {
                            return new NotFoundResult();
                        }
                    }
                    else
                    {
                        if (!RepositoryHandler.IsInitialized(repo) || !RepositoryHandler.Exists(repo, path, rivision))
                        {
                            return new NotFoundResult();
                        }

                        var (data, istext) = RepositoryHandler.ReadFileWithTypeCheck(repo, path, rivision);
                        var contentType = Models.ContentTypeProvider.GetContentType(path, istext ? "text/plain; charset=utf-8" : "application/octet-stream");
                        return File(data, contentType);
                    }
                }
                catch
                {
                    return new NotFoundResult();
                }
            }
            else
            {
                return new NotFoundResult();
            }

        }
    }
}
