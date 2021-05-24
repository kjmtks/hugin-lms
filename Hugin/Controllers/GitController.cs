using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hugin.Models;
using Hugin.Data;
using Hugin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hugin.Controllers
{
    [ApiController]
    public class GitController : BasicAuthenticatableController
    {
        private readonly PermissionProviderService PermissionProvider;
        private readonly LectureHandleService LectureHandler;
        private readonly RepositoryHandleService RepositoryHandler;
        public GitController(PermissionProviderService permissionProvider, UserHandleService userHandler, LectureHandleService lectureHandler, RepositoryHandleService repositoryHandler) : base(userHandler)
        {
            PermissionProvider = permissionProvider;
            LectureHandler = lectureHandler;
            RepositoryHandler = repositoryHandler;
        }


        #region For LectureContents
        [HttpGet("/Git/LectureContents/{owner_account}/{lecture_name}.git/info/refs")]
        public IActionResult contents_info_refs(string owner_account, string lecture_name, [FromQuery]string service)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, loginUser =>
            {
                if ((service == "git-upload-pack" && canPullLectureContents(lecture, loginUser)) || (service == "git-receive-pack" && canPushLectureContents(lecture, loginUser)))
                {
                    return info_refs(RepositoryHandler.GetLectureContentsRepository(lecture), service);
                }
                else
                {
                    return new UnauthorizedResult();
                }
            });
        }

        [HttpPost("/Git/LectureContents/{owner_account}/{lecture_name}.git/git-upload-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult contents_git_upload_pack(string owner_account, string lecture_name, [FromQuery]string service)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault(); 
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async loginUser =>
            {
                if(canPullLectureContents(lecture, loginUser))
                {
                    return await git_upload_pack(RepositoryHandler.GetLectureContentsRepository(lecture));
                }
                else
                {
                    return new UnauthorizedResult();
                }
            });
        }

        [HttpPost("/Git/LectureContents/{owner_account}/{lecture_name}.git/git-receive-pack")]
        [RequestSizeLimit(1_200_000_000)]
        public IActionResult contents_git_receive_pack(string owner_account, string lecture_name, [FromQuery] string service)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault(); 
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async loginUser =>
            {
                if (canPushLectureContents(lecture, loginUser))
                {
                    return await git_receive_pack(RepositoryHandler.GetLectureContentsRepository(lecture));
                }
                else
                {
                    return new UnauthorizedResult();
                }
            });
        }

        private bool canPullLectureContents(Lecture lecture, User loginUser)
        {
            return PermissionProvider.CanReadLectureContentsRepository(lecture, loginUser);
        }
        private bool canPushLectureContents(Lecture lecture, User loginUser)
        {
            return PermissionProvider.CanWriteLectureContentsRepository(lecture, loginUser);
        }

        #endregion


        #region For LectureUserData
        [HttpGet("/Git/LectureUserData/{owner_account}/{lecture_name}/{user_account}.git/info/refs")]
        public IActionResult userData_info_refs(string owner_account, string lecture_name, string user_account, [FromQuery] string service)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            var user = UserHandler.Set.Include(x => x.LectureUserRelationships)
                .Where(x => x.Account == user_account).AsNoTracking().FirstOrDefault();
            if (user == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, loginUser =>
            {
                if ((service == "git-upload-pack" && canPullLectureUserData(lecture, user, loginUser)) || (service == "git-receive-pack" && canPushLectureUserData(lecture, user, loginUser)))
                {
                    return info_refs(RepositoryHandler.GetLectureUserDataRepository(lecture, user_account), service);
                }
                else
                {
                    return new UnauthorizedResult();
                }
            });
        }

        [HttpPost("/Git/LectureUserData/{owner_account}/{lecture_name}/{user_account}.git/git-upload-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult userData_git_upload_pack(string owner_account, string lecture_name, string user_account, [FromQuery] string service)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            var user = UserHandler.Set.Include(x => x.LectureUserRelationships)
                .Where(x => x.Account == user_account).AsNoTracking().FirstOrDefault();
            if (user == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async loginUser =>
            {
                if (canPullLectureUserData(lecture, user, loginUser))
                {
                    return await git_upload_pack(RepositoryHandler.GetLectureUserDataRepository(lecture, user_account));
                }
                else
                {
                    return new UnauthorizedResult();
                }
            });
        }

        [HttpPost("/Git/LectureUserData/{owner_account}/{lecture_name}/{user_account}.git/git-receive-pack")]
        [RequestSizeLimit(1_200_000_000)]
        public IActionResult userData_git_receive_pack(string owner_account, string lecture_name, string user_account, [FromQuery] string service)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Include(x => x.LectureUserRelationships).ThenInclude(x => x.User)
                .Where(x => x.Name == lecture_name && x.Owner.Account == owner_account).AsNoTracking().FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            var user = UserHandler.Set.Include(x => x.LectureUserRelationships)
                .Where(x => x.Account == user_account).AsNoTracking().FirstOrDefault();
            if (user == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async loginUser =>
            {
                if (canPushLectureUserData(lecture, user, loginUser))
                {
                    return await git_receive_pack(RepositoryHandler.GetLectureUserDataRepository(lecture, user_account));
                }
                else
                {
                    return new UnauthorizedResult();
                }
            });
        }

        private bool canPullLectureUserData(Lecture lecture, User user, User loginUser)
        {
            return PermissionProvider.CanReadLectureUserDataRepository(lecture, user, loginUser);
        }
        private bool canPushLectureUserData(Lecture lecture, User user, User loginUser)
        {
            return PermissionProvider.CanWriteLectureUserDataRepository(lecture, user, loginUser);
        }

        #endregion



        private IActionResult info_refs(Repository repository, string service)
        {
            if(service == "git-upload-pack")
            {
                return Content(RepositoryHandler.Pack(repository, PackService.GitUploadPack), "application/x-git-upload-pack-advertisement");
            }
            if (service == "git-receive-pack")
            {
                return Content(RepositoryHandler.Pack(repository, PackService.GitReceivePack), "application/x-git-receive-pack-advertisement");
            }
            throw new ArgumentException($"Invalid service was requested: `{service}'");
        }

        private async Task<IActionResult> git_upload_pack(Repository repository)
        {
            var (_, result) = await RepositoryHandler.Pack(repository, PackService.GitUploadPack, Request.Body);
            return File(result, "application/x-git-upload-pack-result");
        }

        private async Task<IActionResult> git_receive_pack(Repository repository)
        {
            return await RepositoryHandler.DoWithLock(repository, async r =>
            {
                var (noflash, result) = await RepositoryHandler.Pack(r, PackService.GitReceivePack, Request.Body);
                if (!noflash && RepositoryHandler.GetBranches(repository).Contains("master"))
                {
                    RepositoryHandler.PullFromBare(repository, "master");
                }
                return File(result, "application/x-git-receive-pack-result");
            });
        }
    }


}