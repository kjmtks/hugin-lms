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
using System.Diagnostics;

namespace Hugin.Services
{
    public class SandboxHandleService : EntityHandleServiceBase<Sandbox>
    {
        private readonly FilePathResolveService FilePathResolver;
        private readonly RepositoryHandleService RepositoryHandler;

        public override DbSet<Sandbox> Set { get => DatabaseContext.Sandboxes; }

        public override IQueryable<Sandbox> DefaultQuery 
        {
            get => Set.Include(x => x.Lecture).ThenInclude(x => x.Owner);
        }

        public SandboxHandleService(DatabaseContext databaseContext, FilePathResolveService filePathResolver, RepositoryHandleService repositoryHandler)
            : base(databaseContext)
        {
            FilePathResolver = filePathResolver;
            RepositoryHandler = repositoryHandler;
        }

        public IEnumerable<XmlSandbox> GetXmlSandboxes(Data.Lecture lecture, string rivision)
        {
            var repository = RepositoryHandler.GetLectureContentsRepository(lecture);
            if(RepositoryHandler.IsInitialized(repository))
            {
                 return RepositoryHandler.GetFileNames(repository, "sandboxes/", rivision).Select(xml => 
                 {
                     try
                     {
                         var serializer = new XmlSerializer(typeof(XmlSandbox));
                         var text = RepositoryHandler.ReadTextFile(repository, xml, rivision);
                         using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)))
                         {
                             return (XmlSandbox)serializer.Deserialize(ms);
                         }
                     }
                     catch(Exception)
                     {
                         return null;
                     }
                 }).Where(x => x != null);
            }
            else
            {
                return new XmlSandbox[] { };
            }
        }

        public IEnumerable<XmlSandbox> GetUninstalledXmlSandboxes(Data.Lecture lecture, string rivision)
        {
            var xs = DatabaseContext.Sandboxes.Where(x => x.LectureId == lecture.Id).Select(x => x.Name).ToList();
            return GetXmlSandboxes(lecture, rivision).Where(x => !xs.Contains(x.Name));
        }

        [Serializable, XmlRoot(ElementName = "Sandbox")]
        public class XmlSandbox
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Installation { get; set; }
        }


        protected override bool BeforeAddNew(Sandbox model)
        {
            model.State = Sandbox.SandboxState.Uninstalled;
            return true;
        }
        protected override void AfterAddNew(Sandbox model)
        {
            var directoryPath = FilePathResolver.GetSandboxDirectoryPath(model.Lecture.Owner.Account, model.Lecture.Name, model.Name);
            if (Directory.Exists(directoryPath))
            {
                if (Directory.Exists($"{directoryPath}/home"))
                {
                    foreach (var dir in Directory.GetDirectories($"{directoryPath}/home"))
                    {
                        Process.Start("umount", $"{dir}").WaitForExit();
                    }
                }
                Directory.Delete(directoryPath, true);
            }
        }

        protected override bool BeforeRemove(Sandbox model)
        {
            var directoryPath = FilePathResolver.GetSandboxDirectoryPath(model);
            if (Directory.Exists(directoryPath))
            {
                if (Directory.Exists($"{directoryPath}/home"))
                {
                    foreach (var dir in Directory.GetDirectories($"{directoryPath}/home"))
                    {
                        Process.Start("umount", $"{dir}").WaitForExit();
                    }
                }

                Directory.Delete(directoryPath, true);
            }
            return true;
        }

    }
}
