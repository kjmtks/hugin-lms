using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class PermissionProviderService
    {
        private readonly Data.DatabaseContext DatabaseContext;
        private readonly SubmissionHandleService SubmissionHandler;

        public PermissionProviderService(Data.DatabaseContext databaseContext, SubmissionHandleService submissionHandler)
        {
            DatabaseContext = databaseContext;
            SubmissionHandler = submissionHandler;
        }

        public bool CanShowSystemRawFile(Data.User user)
        {
            return user.IsAdmin;
        }
        public bool CanShowJobQueue(Data.User user)
        {
            return user.IsAdmin;
        }
        public bool CanEditSandboxTemplate(Data.User user)
        {
            return user.IsAdmin;
        }
        public bool CanManageUser(Data.User user)
        {
            return user.IsAdmin;
        }

        public bool CanReadLectureContentsRepository(Data.Lecture lecture, Data.User loginUser)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == loginUser.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanReadContentsRepository));
        }
        public bool CanReadLectureContentsRepository(string lectureOwnerAccount, string lectureName, string loginUserAccount)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.Lecture).ThenInclude(x => x.Owner).Include(x => x.User)
                .Any(x => x.Lecture.Owner.Account == lectureOwnerAccount && x.Lecture.Name == lectureName && x.User.Account == loginUserAccount && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanReadContentsRepository));
        }

        public bool CanWriteLectureContentsRepository(Data.Lecture lecture, Data.User loginUser)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == loginUser.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanWriteContentsRepository));
        }
        public bool CanWriteLectureContentsRepository(string lectureOwnerAccount, string lectureName, string loginUserAccount)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.Lecture).ThenInclude(x => x.Owner).Include(x => x.User)
                .Any(x => x.Lecture.Owner.Account == lectureOwnerAccount && x.Lecture.Name == lectureName && x.User.Account == loginUserAccount && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanWriteContentsRepository));
        }


        public bool CanReadLectureUserDataRepository(Data.Lecture lecture, Data.User user, Data.User loginUser)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == loginUser.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowSubmission));
        }
        public bool CanReadLectureUserDataRepository(string lectureOwnerAccount, string lectureName, string userAccount, string loginUserAccount)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.Lecture).ThenInclude(x => x.Owner).Include(x => x.User)
                .Any(x => x.Lecture.Owner.Account == lectureOwnerAccount && x.Lecture.Name == lectureName && x.User.Account == loginUserAccount && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowSubmission));
        }
        public bool CanWriteLectureUserDataRepository(Data.Lecture lecture, Data.User user, Data.User loginUser)
        {
            return false;
        }
        public bool CanWriteLectureUserDataRepository(string lectureOwnerAccount, string lectureName, string userAccount, string loginUserAccount)
        {
            return false;
        }

        public bool CanShowLectureDashboard(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role > Data.LectureUserRelationship.LectureRole.Student);
        }
        public bool CanShowLecturePage(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowPage));
        }
        public bool CanShowLectureUserDataRawFile(Data.Lecture lecture, Data.User targetUser, Data.User user)
        {
            return targetUser.Id == user.Id || DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowSubmission));
        }
        public bool CanShowActivity(Data.Lecture lecture, Data.User user, Models.Activity activity)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowPage));
        }
        public bool CanSubmitActivity(Data.Lecture lecture, Data.User user, Models.Activity activity)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanSubmitActivity))
                && activity.UseSubmit();
        }
        public bool CanAnswerActivity(Data.Lecture lecture, Data.User user, Models.Activity activity)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowSubmission))
                && activity.UseAnswer();
        }

        public bool CanShowLectureUsers(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role > Data.LectureUserRelationship.LectureRole.Student);
        }
        public bool CanEditLectureUsers(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role == Data.LectureUserRelationship.LectureRole.Lecurer);
        }
        public bool CanShowSubmission(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanShowSubmission));
        }

        public bool CanMarkSubmission(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanMarkSubmission));
        }
        public bool CanMarkSubmission(string lectureOwnerAccount, string lectureName, string userAccount)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.Lecture).ThenInclude(x => x.Owner).Include(x => x.User)
                .Any(x => x.Lecture.Owner.Account == lectureOwnerAccount && x.Lecture.Name == lectureName && x.User.Account == userAccount && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanMarkSubmission));
        }
        public bool CanEditSandbox(Data.Lecture lecture, Data.User user)
        {
            return DatabaseContext.LectureUserRelationships.Any(x => x.LectureId == lecture.Id && x.UserId == user.Id && x.Role.HasFlag(Data.LectureUserRelationship.LectureRole.CanEditSandbox));
        }
        public bool CanEditLecture(Data.User user)
        {
            return user.IsTeacher;
        }

        public bool CanShowAllSubmission(Data.User user)
        {
            return SubmissionHandler.UnmarkedLatestEntries(user)?.Any() ?? false;
        }
        
    }
}
