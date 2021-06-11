using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public interface IContentsBuildService
    {
        public Task<string> BuildActivityAsync(LectureHandleService lectureHandler, Models.ActivityProfile prof, Data.User user, Data.Lecture lecture);
        public Task<string> BuildPageAsync(LectureHandleService lectureHandler, Data.User user, Data.Lecture lecture, string rivision, string pagePath);
    }
}
