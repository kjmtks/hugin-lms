using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;
using Microsoft.EntityFrameworkCore;

namespace Hugin.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ContentController : Controller
    {
        private readonly PermissionProviderService PermissionProvider;
        private readonly UserHandleService UserHandler;
        private readonly LectureHandleService LectureHandler;
        private readonly ContentParserService ContentParser;
        private readonly RepositoryHandleService RepositoryHandler;

        public ContentController(PermissionProviderService permissionProvider, UserHandleService userHandler, LectureHandleService lectureHandler, ContentParserService contentParser, RepositoryHandleService repositoryHandler)
        {
            PermissionProvider = permissionProvider;
            UserHandler = userHandler;
            LectureHandler = lectureHandler;
            ContentParser = contentParser;
            RepositoryHandler = repositoryHandler;
        }


        [HttpGet("/Page/{lectureOwner}/{lectureName}/{rivision}/{**path}")]
        public async Task<IActionResult> Page(string lectureOwner, string lectureName, string rivision, string path)
        {
            var loginUser = UserHandler.Set.Where(x => x.Account == User.Identity.Name).AsNoTracking().FirstOrDefault();
            var lecture = LectureHandler.Set.Include(x => x.Owner).Where(x => x.Name == lectureName && x.Owner.Account == lectureOwner && x.IsActived).AsNoTracking().FirstOrDefault();

            var repository = RepositoryHandler.GetLectureContentsRepository(lecture);
            if (RepositoryHandler.IsInitialized(repository) && PermissionProvider.CanShowLecturePage(lecture, loginUser))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path) && RepositoryHandler.Exists(repository, $"pages/{path}", rivision))
                    {
                        if (RepositoryHandler.Exists(repository, "pages/index.md", rivision))
                        {
                            path = "index.md";
                        }
                        else if (RepositoryHandler.Exists(repository, "pages/index.html", rivision))
                        {
                            path = "index.html";
                        }
                        else if (RepositoryHandler.Exists(repository, "pages/index.htm", rivision))
                        {
                            path = "index.htm";
                        }
                    }
                    var ext = path.Split(".").LastOrDefault();
                    if (ext == "md" || ext == "html" || ext == "htm")
                    {
                        var html = await ContentParser.BuildPageAsync(this, loginUser, lecture, rivision, path);
                        return View(new Tuple<Data.Lecture, Data.User, string, string, string>(lecture, loginUser, rivision, path, html));
                    }
                    else
                    {

                        var (data, istext) = RepositoryHandler.ReadFileWithTypeCheck(repository, $"pages/{path}", rivision);

                        if (istext)
                        {
                            return new ContentResult
                            {
                                ContentType = "text/plain; charset=utf-8",
                                Content = System.Text.UTF8Encoding.UTF8.GetString(data),
                            };
                        }
                        else
                        {
                            var contentType = Models.ContentTypeProvider.GetContentType(path);
                            return File(data, contentType);
                        }
                    }
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
    }
}
