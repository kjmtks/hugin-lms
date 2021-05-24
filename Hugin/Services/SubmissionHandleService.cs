using Hugin.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Hugin.Services
{

    public class SubmissionHandleService : EntityHandleServiceBase<Submission>
    {
        private readonly LectureHandleService LectureHandler;

        public SubmissionHandleService(Data.DatabaseContext databaseContext, LectureHandleService lectureHandler) : base(databaseContext)
        {
            LectureHandler = lectureHandler;
        }

        public override DbSet<Submission> Set { get => DatabaseContext.Submissions; }

        public override IQueryable<Submission> DefaultQuery { get => Set.Include(x => x.Lecture).ThenInclude(x => x.Owner).Include(x => x.User); }

       
        public IQueryable<Submission> UnmarkedLatestEntries(User user)
        {
            var xs = LectureHandler.GetLecturesIncludingRole(user, LectureUserRelationship.LectureRole.CanShowSubmission);
            IQueryable<Submission> result = null;
            foreach (var x in xs)
            {
                if(result == null)
                {
                    result = UnmarkedLatestEntries(x);
                }
                else
                {
                    result = result.Concat(UnmarkedLatestEntries(x));
                }
            }
            return result?.OrderByDescending(x => x.SubmittedAt);
        }

        public IQueryable<Submission> UnmarkedLatestEntries(Lecture lecture)
        {
            var xs = Set.Where(x => x.LectureId == lecture.Id);
            return xs.Where(x => x.LectureId == lecture.Id
                    && x.State == Submission.SubmissionState.Submitted
                    && x.SubmittedAt == xs.Where(y => x.LectureId == lecture.Id && x.ActivityName == y.ActivityName && x.UserId == y.UserId).Max(x => x.SubmittedAt))
                .Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .OrderByDescending(x => x.SubmittedAt);
        }

        public IQueryable<Submission> MyLatestSubmissions(User user)
        {
            return Set.Where(x => x.UserId == user.Id && x.SubmittedAt == Set.Where(y => x.State != Submission.SubmissionState.Deleted && x.LectureId == y.Lecture.Id && x.ActivityName == y.ActivityName && x.UserId == y.UserId).Max(x => x.SubmittedAt))
                .Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner);
        }

        public IQueryable<Submission> LatestSubmissions(Lecture lecture, string activityName)
        {
            var _activityName = activityName.Replace("'", "\\'");
            var sql = $@"
SELECT
DISTINCT on (z.""UserId"")
z.*
FROM
(
SELECT *
FROM
(
  SELECT
    t.*
  FROM (
    SELECT
      h.""Id"" AS ""Id"",
      h.""LectureId"" as ""LectureId"",
      h.""UserId"" as ""UserId"",
      h.""ActivityName"" as ""ActivityName"",
      h.""Page"" as ""Page"",
      h.""Tags"" as ""Tags"",
      h.""Hash"" as ""Hash"",
      h.""SubmittedFiles"" as ""SubmittedFiles"",
      h.""SubumitComment"" as ""SubumitComment"",
      h.""SubmittedAt"" as ""SubmittedAt"",
      h.""Deadline"" as ""Deadline"",
      h.""State"" as ""State"",
      h.""Grade"" as ""Grade"",
      h.""FeedbackComment"" as ""FeedbackComment"",
      h.""MarkerUserId"" as ""MarkerUserId"",
      h.""MarkedAt"" as ""MarkedAt"",
      h.""ResubmitDeadline"" as ""ResubmitDeadline"",
      h.""Count"" as ""Count"",
      h.""NumOfSaves"" as ""NumOfSaves"",
      h.""NumOfRuns"" as ""NumOfRuns"",
      h.""NumOfValidateRejects"" as ""NumOfValidateRejects"",
      h.""NumOfValidateAccepts"" as ""NumOfValidateAccepts"",
      row_number() over(partition by h.""UserId"" order by h.""Count"" desc, h.""Id"" desc) as rn,
      h.""Flag""
    FROM
    (
        (
          SELECT
            (SELECT COALESCE(max(""Id""), 0) FROM ""Submissions"") + row_number() OVER () AS ""Id"",
            {lecture.Id} as ""LectureId"",
            u.""Id"" as ""UserId"",
            '{_activityName}' as ""ActivityName"",
            '' as ""Page"",
            '' as ""Tags"",
            '' as ""Hash"",
            '' as ""SubmittedFiles"",
            '' as ""SubumitComment"",
            (date '2020-01-01') as ""SubmittedAt"",
            (date '2020-01-01') as ""Deadline"",
            0 as ""State"",
            '' as ""Grade"",
            '' as ""FeedbackComment"",
            0 as ""MarkerUserId"",
            (date '2020-01-01') as ""MarkedAt"",
            (date '2020-01-01') as ""ResubmitDeadline"",
            0 as ""Count"",
            0 as ""NumOfSaves"",
            0 as ""NumOfRuns"",
            0 as ""NumOfValidateRejects"",
            0 as ""NumOfValidateAccepts"",
            0 as ""Flag""
          FROM
            (SELECT ""Id"" FROM ""Users"" AS s INNER JOIN ""LectureUserRelationships"" AS a ON a.""UserId"" = s.""Id"" AND a.""LectureId"" = {lecture.Id}) as u
        )
        UNION ALL
        (
            SELECT
              t.""Id"" AS ""Id"",
              t.""LectureId"" as ""LectureId"",
              t.""UserId"" as ""UserId"",
              t.""ActivityName"" as ""ActivityName"",
              t.""Page"" as ""Page"",
              t.""Tags"" as ""Tags"",
              t.""Hash"" as ""Hash"",
              t.""SubmittedFiles"" as ""SubmittedFiles"",
              t.""SubumitComment"" as ""SubumitComment"",
              t.""SubmittedAt"" as ""SubmittedAt"",
              t.""Deadline"" as ""Deadline"",
              t.""State"" as ""State"",
              t.""Grade"" as ""Grade"",
              t.""FeedbackComment"" as ""FeedbackComment"",
              t.""MarkerUserId"" as ""MarkerUserId"",
              t.""MarkedAt"" as ""MarkedAt"",
              t.""ResubmitDeadline"" as ""ResubmitDeadline"",
              t.""Count"" as ""Count"",
              t.""NumOfSaves"" as ""NumOfSaves"",
              t.""NumOfRuns"" as ""NumOfRuns"",
              t.""NumOfValidateRejects"" as ""NumOfValidateRejects"",
              t.""NumOfValidateAccepts"" as ""NumOfValidateAccepts"",
              1 as ""Flag""
            FROM ""Submissions"" as t
            INNER JOIN ""LectureUserRelationships"" AS b ON b.""UserId"" = t.""UserId"" AND b.""LectureId"" = {lecture.Id}
            WHERE t.""ActivityName"" = '{_activityName}' AND t.""LectureId"" = {lecture.Id} AND t.""State"" <> {(int)Submission.SubmissionState.Deleted}
        )
    ) as h
    WHERE h.""ActivityName"" = '{_activityName}' AND h.""LectureId"" = {lecture.Id}
  ) as t
  WHERE t.""rn"" = 1
) as z
ORDER BY z.""Flag"" desc, z.""SubmittedAt"" desc
) as z
";
            return Set.FromSqlRaw(sql);
        }
    }
}
