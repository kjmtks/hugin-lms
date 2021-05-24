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

namespace Hugin.Services
{
    public class SandboxTemplateHandleService : EntityHandleServiceBase<SandboxTemplate>
    {

        public SandboxTemplateHandleService(DatabaseContext databaseContext) : base(databaseContext)
        {
        }

        public override DbSet<SandboxTemplate> Set { get => DatabaseContext.SandboxTemplates; }

        public override IQueryable<SandboxTemplate> DefaultQuery { get => Set; }
    }
}
