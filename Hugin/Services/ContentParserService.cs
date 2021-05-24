using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace Hugin.Services
{

    public class ContentParserService
    {
        private readonly LectureHandleService LectureHandler;
        private readonly ActivityEncryptService ActivityEncryptor;
        private readonly RazorBuildService RazorBuilder;
        private readonly RepositoryHandleService RepositoryHandler;
        private readonly PermissionProviderService PermissionProvider;
        public ContentParserService(PermissionProviderService permissionProvider, LectureHandleService lectureHandler, ActivityEncryptService activityEncryptor, RazorBuildService razorBuilder, RepositoryHandleService repositoryHandler)
        {
            LectureHandler = lectureHandler;
            ActivityEncryptor = activityEncryptor;
            RazorBuilder = razorBuilder;
            RepositoryHandler = repositoryHandler;
            PermissionProvider = permissionProvider;
        }

        class Dummy1 { }
        class Dummy2 { }

        private async Task<string> embedActivityAsync(Microsoft.AspNetCore.Mvc.Controller controller, Data.User user, Data.Lecture lecture, string activityRef, string rivision, string pagePath, Dictionary<string, string> activityParameters, int cnt)
        {
            var prof = new Models.ActivityProfile
            {
                Number = cnt,
                UserAccount = user.Account,
                LectureOwnerAccount = lecture.Owner.Account,
                LectureName = lecture.Name,
                ActivityRef = activityRef,
                Rivision = rivision,
                PagePath = pagePath,
                Parameters = activityParameters,
            };


            var (activity, _) = await BuildActivityAsync(user, prof);
            var sprof = ActivityEncryptor.Encrypt(prof);

            var id = Guid.NewGuid();

            string desc = null;
            if (!string.IsNullOrWhiteSpace(activity?.Description?.Text))
            {
                desc = await compileToHTML(activity.Description.UseMarkdown, user, lecture, prof.Rivision, prof.PagePath, activity.Description.Text.Trim());
            }

            var vm = new ViewModels.ActivityViewModel()
            {
                Id = id,
                Profile = prof,
                Activity = activity,
                EncryptedProfile = sprof,
                Description = desc
            };
            return renderViewToStringAsync(controller, "/Views/Content/Activity.cshtml", vm).Result;

        }

        public async Task<(Models.Activity, string)> BuildActivityAsync(Data.User user, Models.ActivityProfile prof)
        {
            var lecture = LectureHandler.Set.Include(x => x.Owner).Where(x => x.Owner.Account == prof.LectureOwnerAccount && x.Name == prof.LectureName && x.IsActived).AsNoTracking().FirstOrDefault();
            if (lecture == null)
            {
                throw new UnauthorizedAccessException();
            }

            var repository = RepositoryHandler.GetLectureContentsRepository(prof.LectureOwnerAccount, prof.LectureName);


            var commitInfo = RepositoryHandler.ReadCommitInfo(repository, $"activities/{prof.ActivityRef}", prof.Rivision);


            dynamic viewbag = new System.Dynamic.ExpandoObject();
            var x = viewbag as IDictionary<string, Object>;
            foreach (var p in LectureHandler.GetLectureParameters(lecture, prof.Rivision))
            {
                x.Add(p.Key, p.Value.GetValue());
            }
            foreach (var p in prof.Parameters)
            {
                x.Add(p.Key, p.Value);
            }
            var model = new Models.PageModel(RepositoryHandler, LectureHandler, repository, user, lecture, prof.Rivision, prof.PagePath, commitInfo, viewbag);

            string xml;

            var page_hash = RepositoryHandler.GetHashOfLatestCommit(repository, $"pages/{prof.PagePath}", prof.Rivision);
            var activity_hash = RepositoryHandler.GetHashOfLatestCommit(repository, $"activities/{prof.ActivityRef}", prof.Rivision);
            var key = $"{prof.LectureOwnerAccount}/{prof.LectureName}/{prof.PagePath}:{page_hash}/{prof.Number}:{activity_hash}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            //sb.AppendLine("@{ DisableEncoding = true; }");
            sb.Append(RepositoryHandler.ReadTextFile(repository, $"activities/{prof.ActivityRef}", prof.Rivision));
            xml = await RazorBuilder.CompileAsync(key, sb.ToString(), model, viewbag);

            /*
            XmlSchemaSet set = new XmlSchemaSet();
            set.Add("urn:activity-schema", $"ActivitySchema.xsd");
            XmlSchema schema = null;
            foreach (XmlSchema s in set.Schemas("urn:activity-schema"))
            {
                schema = s;
            }

            XmlDocument xdoc = new XmlDocument();
            xdoc.Schemas.Add(schema);
            xdoc.LoadXml(xml);
            var sb2 = new System.Text.StringBuilder();
            xdoc.Validate((sender, args) => {
                if (args.Severity == XmlSeverityType.Error)
                {
                    sb2.AppendLine("Error: " + args.Message);
                }
            });
            var validateResult = sb2.ToString().TrimEnd();
            if (validateResult != string.Empty)
            {
                throw new FormatException(validateResult);
            }
            */

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Models.Activity));
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)))
            {
                var activity = (Models.Activity)serializer.Deserialize(ms);
                prof.ActivityName = activity.Name;
                prof.CanUseSubmit = PermissionProvider.CanSubmitActivity(lecture, user, activity);
                prof.CanUseAnswer = PermissionProvider.CanAnswerActivity(lecture, user, activity);
                return (activity, xml);
            }
        }

        private async Task<string> renderViewToStringAsync<TModel>(Microsoft.AspNetCore.Mvc.Controller controller, string viewNamePath, TModel model)
        {
            if (string.IsNullOrWhiteSpace(viewNamePath))
            {
                viewNamePath = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (StringWriter writer = new StringWriter())
            {

                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = null;

                if (viewNamePath.EndsWith(".cshtml"))
                    viewResult = viewEngine.GetView(viewNamePath, viewNamePath, false);
                else
                    viewResult = viewEngine.FindView(controller.ControllerContext, viewNamePath, false);

                if (!viewResult.Success)
                    return $"A view with the name '{viewNamePath}' could not be found";

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );
                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();

            }
        }


        public async Task<string> BuildPageAsync(Microsoft.AspNetCore.Mvc.Controller controller, Data.User user, Data.Lecture lecture, string rivision, string pagePath)
        {
            var repository = RepositoryHandler.GetLectureContentsRepository(lecture);

            var commitInfo = RepositoryHandler.ReadCommitInfo(repository, $"pages/{pagePath}", rivision);

            dynamic viewbag = new System.Dynamic.ExpandoObject();
            var x = viewbag as IDictionary<string, Object>;
            foreach (var p in LectureHandler.GetLectureParameters(lecture, rivision))
            {
                x.Add(p.Key, p.Value.GetValue());
            }
            var model = new Models.PageModel(RepositoryHandler, LectureHandler, repository, user, lecture, rivision, pagePath, commitInfo, viewbag);


            var hash = RepositoryHandler.GetHashOfLatestCommit(repository, $"pages/{pagePath}", rivision);
            var key = $"{lecture.Owner.Account}/{lecture.Name}/{pagePath}:{rivision}:{hash}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            //sb.AppendLine("@{ DisableEncoding = true; }");
            sb.Append(RepositoryHandler.ReadTextFile(repository, $"pages/{pagePath}", rivision));

            var page = await RazorBuilder.CompileAsync(key, sb.ToString(), model, viewbag);

            try
            {
                var file = new System.IO.FileInfo(pagePath);

                if (file.Extension.ToLower() == ".md" || file.Extension.ToLower() == ".html" || file.Extension.ToLower() == ".htm")
                {
                    return await compileToHTML(file.Extension == ".md" , user, lecture, rivision, pagePath, page, controller);
                }
                else
                {
                    return $"<article class=\"message is-danger\"><div class=\"message-body\">Invalid File</div></article>";
                }
            }
            catch (FileNotFoundException)
            {
                return $"<article class=\"message is-danger\"><div class=\"message-body\">Not found</div></article>";
            }
        }

        private async Task<string> compileToHTML(bool isMarkDown, Data.User user, Data.Lecture lecture, string rivision, string pagepath, string text, Microsoft.AspNetCore.Mvc.Controller controller = null)
        {
            if(isMarkDown)
            {
                var r = new Markdig.MarkdownPipelineBuilder();
                var q = Markdig.MarkdownExtensions.UseAdvancedExtensions(r);
                var p = q.Build();
                text = Markdig.Markdown.ToHtml(text
                    .Replace("\\[", "\\\\[").Replace("\\]", "\\\\]")
                    .Replace("\\(", "\\\\(").Replace("\\)", "\\\\)")
                    .Replace("\\{", "\\\\{").Replace("\\}", "\\\\}"), p);
            }
            var options = new AngleSharp.Html.Parser.HtmlParserOptions();
            var parser = new AngleSharp.Html.Parser.HtmlParser(options);
            var doc = parser.ParseDocument(text);
            return await compileAsync(isMarkDown, controller, doc.Body, user, lecture, rivision, pagepath, 0);
        }

        private async Task<string> compileAsync(bool isMarkDown, Microsoft.AspNetCore.Mvc.Controller controller, AngleSharp.Dom.IElement element, Data.User user, Data.Lecture lecture, string rivision, string pagepath, int cnt = 0, int depth = 0)
        {
            var html = new StringBuilder();
            var attrs = new StringBuilder();

            foreach (var e in element.ChildNodes)
            {
                if (e is AngleSharp.Dom.IElement el)
                {
                    if (controller != null && el.TagName == "SCRIPT" && el.GetAttribute("language") == "activity")
                    {
                        var reff = el.GetAttribute("ref")?.Replace("\"", "\\\"");
                        try
                        {
                            var id = Guid.NewGuid();
                            var parameters = parseParameters(el.InnerHtml);
                            html.Append(await embedActivityAsync(controller, user, lecture, reff, rivision, pagepath, parameters, cnt));
                            cnt++;
                        }
                        catch (Exception ex)
                        {  
                            html.Append($"<article class=\"message is-danger\"><div class=\"message-header\">Activity Error</div><div class=\"message-body\">{ex.Message}</div></article>");
                        }
                    }
                    else if (el.TagName == "IMG")
                    {
                        attrs.Clear();
                        foreach (var attr in el.Attributes.Where(x => x.Name != "src" && x.Name != "alt"))
                        {
                            attrs.Append($"{attr.Name}=\"{attr.Value.Replace("\"", "\\\"")}\" ");
                        }
                        if (el.HasAttribute("src"))
                        {
                            var src = el.GetAttribute("src");
                            if (!Regex.IsMatch(src, "^[a-zA-Z0-9]+://"))
                            {
                                src = $"/Page/{lecture.Owner.Account}/{lecture.Name}/{rivision}/{relPathToAbsPath(pagepath, src)}";
                            }
                            attrs.Append($"src=\"{src.Replace("\"", "\\\"")}\" ");
                        }
                        if (isMarkDown && el.HasAttribute("alt"))
                        {
                            var alt = el.GetAttribute("alt").Trim();
                            if(!string.IsNullOrWhiteSpace(el.GetAttribute("alt")))
                            {
                                if (Regex.IsMatch(alt, @"^\^"))
                                {
                                    var _alt = Regex.Replace(alt, @"^\^", "");
                                    html.Append($"<div class=\"page-figure\"><span class=\"page-caption\">{_alt}</span><{el.TagName} {attrs.ToString()} /></div>");
                                }
                                else
                                {
                                    html.Append($"<div class=\"page-figure\"><{el.TagName} {attrs.ToString()} /><span class=\"page-caption\">{alt}</span></div>");
                                }
                            }
                            else
                            {
                                html.Append($"<div class=\"page-figure\"><{el.TagName} {attrs.ToString()} /></div>");
                            }
                        }
                        else
                        {
                            html.Append($"<{el.TagName} {attrs.ToString()} />");
                        }
                    }
                    else
                    {
                        attrs.Clear();
                        if (el.TagName == "A")
                        {
                            foreach (var attr in el.Attributes.Where(x => x.Name != "href"))
                            {
                                attrs.Append($"{attr.Name}=\"{attr.Value.Replace("\"", "\\\"")}\" ");
                            }
                            if (el.HasAttribute("href"))
                            {
                                var href = el.GetAttribute("href");
                                if (Regex.IsMatch(href, "^#"))
                                {
                                    href = $"/Page/{lecture.Owner.Account}/{lecture.Name}/{rivision}/{pagepath}{href}";
                                }
                                else if (!Regex.IsMatch(href, "^[a-zA-Z0-9]+://"))
                                {
                                    href = $"/Page/{lecture.Owner.Account}/{lecture.Name}/{rivision}/{relPathToAbsPath(pagepath, href)}";
                                }
                                attrs.Append($"href=\"{href.Replace("\"", "\\\"")}\" ");
                            }
                        }
                        else
                        {
                            foreach (var attr in el.Attributes)
                            {
                                attrs.Append($"{attr.Name}=\"{attr.Value.Replace("\"", "\\\"")}\" ");
                            }
                        }

                        if (!el.Flags.HasFlag(AngleSharp.Dom.NodeFlags.SelfClosing))
                        {
                            html.Append($"<{el.TagName} {attrs.ToString()}>");
                            html.Append(await compileAsync(isMarkDown, controller, el, user, lecture, rivision, pagepath, cnt, depth + 1));
                            html.Append($"</{el.TagName}>");
                        }
                        else
                        {
                            html.Append($"<{el.TagName} {attrs.ToString()} />");
                        }
                    }

                }
                else if (e is AngleSharp.Dom.IComment)
                {
                    // Comment Element
                }
                else
                {
                    html.Append(escapeForXml(e.TextContent));
                }
            }
            return html.ToString();
        }






        private Dictionary<string, string> parseParameters(string code)
        {
            var result = new Dictionary<string, string>();
            var reg = new Regex(@"<(?<tagname>[a-zA-Z_][a-zA-Z0-9_]+)>(?<value>.*)?</\k<tagname>>", RegexOptions.Singleline);
            foreach (Match m in reg.Matches(code))
            {
                result.Add(m.Groups["tagname"].Value, escapeForXml(m.Groups["value"].Value));
            }
            return result;
        }
        private string escapeForXml(string source)
        {
            return source.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
        private string relPathToAbsPath(string cur, string to)
        {
            string path;
            if (to[0] == '/')
            {
                path = to.Substring(1);
            }
            else
            {
                var xs = cur.Split("/").Where(x => !string.IsNullOrWhiteSpace(x));
                if (xs.Count() <= 1)
                {
                    path = to;
                }
                else
                {
                    path = string.Join("/", xs.Take(xs.Count() - 1)) + "/" + to;
                }
            }

            return path;
        }
    }

}
