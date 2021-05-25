using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Hugin.Data;
using System.Security.Cryptography;
using System.Text;
using Hugin.Models;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Hugin.Services
{
    public class LectureHandleService : EntityHandleServiceBase<Lecture>
    {
        private readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly FilePathResolveService FilePathResolver;
        private readonly RepositoryHandleService RepositoryHandler;

        public LectureHandleService(IServiceScopeFactory serviceScopeFactory, DatabaseContext databaseContext, FilePathResolveService filePathResolver, RepositoryHandleService repositoryHandler)
            : base(databaseContext)
        {
            ServiceScopeFactory = serviceScopeFactory;
            FilePathResolver = filePathResolver;
            RepositoryHandler = repositoryHandler;
        }

        public override DbSet<Lecture> Set { get => DatabaseContext.Lectures; }
        public override IQueryable<Lecture> DefaultQuery { get => Set.Include(x => x.Owner); }

        public void RemoveUser(Lecture lecture, User user)
        {
            var rel = DatabaseContext.LectureUserRelationships.Where(x => x.LectureId == lecture.Id && x.UserId == user.Id).FirstOrDefault();
            if (rel != null)
            {
                lock (DatabaseContext)
                {
                    DatabaseContext.LectureUserRelationships.Remove(rel);
                    DatabaseContext.SaveChanges();
                }
            }
        }
        public bool AddUser(Lecture lecture, string account, Data.LectureUserRelationship.LectureRole role)
        {
            lock (DatabaseContext)
            {
                var user = DatabaseContext.Users.Where(x => x.Account == account).FirstOrDefault();
                if (user != null)
                {
                    var rel = DatabaseContext.LectureUserRelationships.Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
                    if (rel != null)
                    {
                        rel.Role = role;
                        DatabaseContext.LectureUserRelationships.Update(rel);
                    }
                    else
                    {
                        DatabaseContext.LectureUserRelationships.Add(new LectureUserRelationship
                        {
                            UserId = user.Id,
                            LectureId = lecture.Id,
                            Role = role
                        });
                    }
                    DatabaseContext.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public IEnumerable<string> AddUsers(Lecture lecture, IEnumerable<string> accounts, Data.LectureUserRelationship.LectureRole role)
        {
            lock (DatabaseContext)
            {
                var errors = new List<string>();
                foreach (var account in accounts)
                {
                    var user = DatabaseContext.Users.Where(x => x.Account == account).FirstOrDefault();
                    if (user != null)
                    {
                        var rel = DatabaseContext.LectureUserRelationships.Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
                        if (rel != null)
                        {
                            rel.Role = role;
                            DatabaseContext.LectureUserRelationships.Update(rel);
                        }
                        else
                        {
                            DatabaseContext.LectureUserRelationships.Add(new LectureUserRelationship
                            {
                                UserId = user.Id,
                                LectureId = lecture.Id,
                                Role = role
                            });
                        }
                    }
                    else
                    {
                        errors.Add(account);
                    }
                }
                DatabaseContext.SaveChanges();
                return errors;
            }
        }

        protected override void AfterAddNew(Lecture model)
        {
            var path = FilePathResolver.GetLectureDirectoryPath(model.Owner.Account, model.Name);
            if (Directory.Exists(path))
            {
                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    var handler = scope.ServiceProvider.GetService<SandboxHandleService>();
                    foreach (var x in handler.Set.Where(x => x.LectureId == model.Id).ToList())
                    {
                        handler.Remove(x);
                    }
                }
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
            Directory.CreateDirectory($"{path}/sandboxes");
            Directory.CreateDirectory($"{path}/users");
            var repository = RepositoryHandler.GetLectureContentsRepository(model.Owner.Account, model.Name);
            RepositoryHandler.Create(repository, model.DefaultBranch);

            lock (DatabaseContext)
            {
                DatabaseContext.LectureUserRelationships.Add(new LectureUserRelationship
                {
                    UserId = model.Owner.Id,
                    LectureId = model.Id,
                    Role = LectureUserRelationship.LectureRole.Lecurer
                });
                DatabaseContext.SaveChanges();
            }

        }

        protected override bool BeforeRemove(Lecture model)
        {
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var sandboxHandler = scope.ServiceProvider.GetService<SandboxHandleService>();
                foreach (var x in sandboxHandler.Set.Where(x => x.LectureId == model.Id).ToList())
                {
                    sandboxHandler.Remove(x);
                }
                var submissionHandler = scope.ServiceProvider.GetService<SandboxHandleService>();
                foreach (var x in submissionHandler.Set.Where(x => x.LectureId == model.Id).ToList())
                {
                    submissionHandler.Remove(x);
                }
                var activityActionHandler = scope.ServiceProvider.GetService<SandboxHandleService>();
                foreach (var x in activityActionHandler.Set.Where(x => x.LectureId == model.Id).ToList())
                {
                    activityActionHandler.Remove(x);
                }
            }

            lock (DatabaseContext)
            {
                DatabaseContext.LectureUserRelationships.RemoveRange(DatabaseContext.LectureUserRelationships.Where(x => x.LectureId == model.Id));
                DatabaseContext.SaveChanges();
            }

            var path = FilePathResolver.GetLectureDirectoryPath(model.Owner.Account, model.Name);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            return true;
        }


        public IQueryable<LectureUserRelationship> GetLectureUserRelationships(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User).Where(x => x.LectureId == lecture.Id);
        }
        public IQueryable<User> GetUsers(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role != LectureUserRelationship.LectureRole.Banned)
                .Select(x => x.User);
        }
        public IEnumerable<(User, LectureUserRelationship.LectureRole)> GetUserAndRoles(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role != LectureUserRelationship.LectureRole.Banned)
                .AsNoTracking().AsEnumerable().Select(x => (x.User, x.Role));
        }
        public IQueryable<User> GetStudents(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role == LectureUserRelationship.LectureRole.Student)
                .Select(x => x.User);
        }
        public IQueryable<User> GetObservers(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role == LectureUserRelationship.LectureRole.Observer)
                .Select(x => x.User);
        }
        public IQueryable<User> GetAssistants(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role == LectureUserRelationship.LectureRole.Assistant)
                .Select(x => x.User);
        }
        public IQueryable<User> GetEditors(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role == LectureUserRelationship.LectureRole.Editor)
                .Select(x => x.User);
        }
        public IQueryable<User> GetLecturers(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role == LectureUserRelationship.LectureRole.Lecurer)
                .Select(x => x.User);
        }
        public IQueryable<User> GetStaffs(Lecture lecture)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role > LectureUserRelationship.LectureRole.Student && x.Role != LectureUserRelationship.LectureRole.Banned)
                .Select(x => x.User);
        }
        public IQueryable<User> GetUsersWhoHasRole(Lecture lecture, LectureUserRelationship.LectureRole role)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.User)
                .Where(x => x.LectureId == lecture.Id && x.Role.HasFlag(role) && x.Role != LectureUserRelationship.LectureRole.Banned)
                .Select(x => x.User);
        }

        public IQueryable<Lecture> GetTeachingLectures(User user)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.UserId == user.Id && x.Role > LectureUserRelationship.LectureRole.Student && x.Role != LectureUserRelationship.LectureRole.Banned)
                .Select(x => x.Lecture);
        }
        public IQueryable<Lecture> GetLecturesIncludingRole(User user, LectureUserRelationship.LectureRole role)
        {
            return DatabaseContext.LectureUserRelationships.Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.UserId == user.Id && x.Role.HasFlag(role) && x.Role != LectureUserRelationship.LectureRole.Banned)
                .Select(x => x.Lecture);
        }
        public IDictionary<string, ILectureParameter> GetLectureParameters(Data.Lecture lecture, string rivision)
        {
            var repository = RepositoryHandler.GetLectureContentsRepository(lecture);
            if (!RepositoryHandler.Exists(repository, "parameters.xml", rivision))
            {
                return new Dictionary<string, ILectureParameter>();
            }
            var (data, isTextFile) = RepositoryHandler.ReadFileWithTypeCheck(repository, "parameters.xml", rivision);
            if (isTextFile)
            {
                var serializer = new XmlSerializer(typeof(LectureParameters));
                var viewbag = new Dictionary<string, ILectureParameter>();
                var parameters = (LectureParameters)serializer.Deserialize(new MemoryStream(data));
                foreach (var p in parameters.GetValues())
                {
                    viewbag.Add(p.Name, p);
                }
                return viewbag;
            }
            else
            {
                return new Dictionary<string, ILectureParameter>();
            }
        }

    }
}
