using Hugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public interface IContentsBuildService
    {
        public Task<string> BuildActivityAsync(LectureHandleService lectureHandler, Models.ActivityProfile prof, Data.User user, Data.Lecture lecture);
        public Task<string> BuildPageAsync(LectureHandleService lectureHandler, Data.User user, Data.Lecture lecture, string rivision, string pagePath);
    }

    public class ScribanContentsBuildService : IContentsBuildService
    {
        private readonly RepositoryHandleService repositoryHandler;

        public ScribanContentsBuildService(RepositoryHandleService _repositoryHandler)
        {
            repositoryHandler = _repositoryHandler;
        }

        public async Task<string> BuildActivityAsync(LectureHandleService lectureHandler, Models.ActivityProfile prof, User user, Lecture lecture)
        {
            var repository = repositoryHandler.GetLectureContentsRepository(lecture);
            var raw = repositoryHandler.ReadTextFile(repository, $"activities/{prof.ActivityRef}", prof.Rivision);
            var template = Scriban.Template.Parse(raw);

            var context = new Scriban.TemplateContext();
            var model = buildModel(lectureHandler, lecture, prof.Rivision, prof);
            context.PushGlobal(model);

            return await template.RenderAsync(context);
        }

        public async Task<string> BuildPageAsync(LectureHandleService lectureHandler, User user, Lecture lecture, string rivision, string pagePath)
        {
            var repository = repositoryHandler.GetLectureContentsRepository(lecture);
            var raw = repositoryHandler.ReadTextFile(repository, $"pages/{pagePath}", rivision);
            var template = Scriban.Template.Parse(raw);

            var context = new Scriban.TemplateContext();
            var model = buildModel(lectureHandler, lecture, rivision);
            context.PushGlobal(model);

            return await template.RenderAsync(context);
        }

        private Scriban.Runtime.ScriptObject buildModel(LectureHandleService lectureHandler, Lecture lecture, string rivision, Models.ActivityProfile prof = null)
        {
            var model = new Scriban.Runtime.ScriptObject();
            foreach (var p in lectureHandler.GetLectureParameters(lecture, rivision))
            {
                model.Add(p.Key, p.Value.GetValue());
            }
            if(prof != null)
            {
                foreach (var p in prof.Parameters)
                {
                    model.Add(p.Key, p.Value);
                }
            }
            return model;
        }
    }

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
            var model = new Models.PageModel(repositoryHandler, lectureHandler, repository, user, lecture, prof.Rivision, prof.PagePath, commitInfo, viewbag);

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
            var model = new Models.PageModel(repositoryHandler, lectureHandler, repository, user, lecture, rivision, pagePath, commitInfo, viewbag);


            var hash = repositoryHandler.GetHashOfLatestCommit(repository, $"pages/{pagePath}", rivision);
            var key = $"{lecture.Owner.Account}/{lecture.Name}/{pagePath}:{rivision}:{hash}";
            var sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            //sb.AppendLine("@{ DisableEncoding = true; }");
            sb.Append(repositoryHandler.ReadTextFile(repository, $"pages/{pagePath}", rivision));

            return await engine.CompileRenderStringAsync(key, sb.ToString(), model, viewbag);
        }
    }
}
