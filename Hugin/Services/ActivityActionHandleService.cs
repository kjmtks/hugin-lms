using Hugin.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class ActivityActionHandleService : EntityHandleServiceBase<ActivityAction>
    {
        public ActivityActionHandleService(DatabaseContext databaseContext) : base(databaseContext)
        {
        }

        public override DbSet<ActivityAction> Set { get => DatabaseContext.ActivityActions; }

        public override IQueryable<ActivityAction> DefaultQuery { get => Set.Include(x => x.Lecture).ThenInclude(x => x.Owner).Include(x => x.User); }

        public enum StateFilter
        {
            All, Save, Run, Validate, Reject, Accept, Submit
        }
        public IEnumerable<(DateTime, long)> GetRequestCounts(string activityName, Data.Lecture lecture, Data.User user, DateTime startAt, DateTime endAt, long spanMinutes, StateFilter filter)
        {
            var condts = new List<string>();
            if (lecture != null)
            {
                condts.Add($@"t0.""LectureId"" = {lecture.Id}");
            }
            if (user != null)
            {
                condts.Add($@"t0.""UserId"" = {user.Id}");
            }
            if(filter == StateFilter.Save)
            {
                condts.Add($@"t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.Save}");
            }
            if (filter == StateFilter.Run)
            {
                condts.Add($@"t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.Run}");
            }
            if (filter == StateFilter.Validate)
            {
                condts.Add($@"(t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.ValidationAccept} OR t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.ValidationReject})");
            }
            if (filter == StateFilter.Reject)
            {
                condts.Add($@"t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.ValidationReject}");
            }
            if (filter == StateFilter.Accept)
            {
                condts.Add($@"t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.ValidationAccept}");
            }
            if (filter == StateFilter.Submit)
            {
                condts.Add($@"t0.""ActivityActionType"" = {(int)Models.ActivityActionTypes.Submit}");
            }
            if(!string.IsNullOrWhiteSpace(activityName))
            {
                condts.Add($@"t0.""ActivityName"" = '{activityName.Replace("'", "\\'")}'");
            }
            var where = string.Join(" AND ", condts);
            if(!string.IsNullOrWhiteSpace(where))
            {
                where = $"WHERE {where}";
            }

            var rawSQL = $@"
SELECT t2.""StartAt"", count(t1.*)
FROM (
  SELECT t0.""LectureId"", t0.""ActivityActionType"", t0.""RequestedAt"" FROM ""ActivityActions"" as t0 {where}
) as t1
RIGHT JOIN (
  SELECT generate_series(
    '{startAt.Year}-{startAt.Month}-{startAt.Day} {startAt.Hour}:{startAt.Minute}:{startAt.Second}',
    '{endAt.Year}-{endAt.Month}-{endAt.Day} {endAt.Hour}:{endAt.Minute}:{endAt.Second}', 
    '{spanMinutes} min'::interval
  )::timestamp as ""StartAt""
) as t2
ON t1.""RequestedAt"" between t2.""StartAt"" and (t2.""StartAt"" + '{spanMinutes} min'::interval)
GROUP BY t2.""StartAt""
ORDER BY t2.""StartAt"";";
            using (var command = DatabaseContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = rawSQL;
                command.CommandType = CommandType.Text;
                DatabaseContext.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        yield return ((DateTime)result[0] ,(long)result[1]);
                    }
                }
            }
        }
    }
}
