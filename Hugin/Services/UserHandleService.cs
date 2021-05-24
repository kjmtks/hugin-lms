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
using Microsoft.Extensions.DependencyInjection;

namespace Hugin.Services
{
    public class UserHandleService : EntityHandleServiceBase<User>
    {
        private readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly ApplicationConfigurationService Conf;
        private readonly FilePathResolveService FilePathResolver;
        public UserHandleService(IServiceScopeFactory serviceScopeFactory, DatabaseContext databaseContext, ApplicationConfigurationService conf, FilePathResolveService filePathResolver)
            : base(databaseContext)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Conf = conf;
            FilePathResolver = filePathResolver;
        }

        public override DbSet<User> Set { get => DatabaseContext.Users; }

        public override IQueryable<User> DefaultQuery { get => Set; }

        public IQueryable<Lecture> GetAttendances(User user)
        {
            return DatabaseContext.LectureUserRelationships.Where(x => x.UserId == user.Id).Include(x => x.Lecture).ThenInclude(x => x.Owner).Select(x => x.Lecture);
        }

        public IEnumerable<string> AddNewLdapUsers(string accounts_string)
        {
            var errors = new List<string>();
            var accounts = accounts_string.Split(new char[] { ',', ' ', '\t', '\n', '\r', ';', ':' }).Where(x => !string.IsNullOrWhiteSpace(x));
            foreach (var account in accounts)
            {
                try
                {
                    AddNew(new User
                    {
                        Account = account,
                        DisplayName = account,
                        EnglishName = account,
                        Email = $"{account}@localhost",
                        IsLdapInitialized = false,
                        IsAdmin = false,
                        IsTeacher = false,
                        IsLdapUser = true,
                    });
                }catch (Exception)
                {
                    errors.Add(account);
                }
            }
            return errors;
        }

        protected override void AfterAddNew(User model)
        {
            var path = FilePathResolver.GetUserDirectoryPath(model.Account);
            if (Directory.Exists(path))
            {
                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    var handler = scope.ServiceProvider.GetService<LectureHandleService>();
                    foreach (var x in handler.Set.Where(x => x.OwnerId == model.Id).ToList())
                    {
                        handler.Remove(x);
                    }
                }
                Directory.Delete(path, true);
            }
        }

        protected override bool BeforeAddNew(User model)
        {
            if(model.IsLdapUser)
            {
                return true;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.RawPassword) && model.RawPassword.Length >= 8)
                {
                    model.EncryptedPassword = Encrypt(model.RawPassword);
                    return true;
                }
                else
                {
                    throw new Exception("Password needs 8 or more characters.");
                }
            }
        }
        protected override bool BeforeUpdate(User model)
        {
            if (model.IsLdapUser)
            {
                model.EncryptedPassword = "";
                return true;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.RawPassword) && model.RawPassword.Length >= 8)
                {
                    model.EncryptedPassword = Encrypt(model.RawPassword);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        protected override bool BeforeRemove(User model)
        {
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var handler = scope.ServiceProvider.GetService<LectureHandleService>();
                foreach (var x in handler.Set.Where(x => x.OwnerId == model.Id).ToList())
                {
                    handler.Remove(x);
                }
            }

            var path = FilePathResolver.GetUserDirectoryPath(model.Account);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            return true;
        }

        public bool Authenticate(User user, string password)
        {
            if (user.IsLdapUser)
            {
                var ldap_host = Environment.GetEnvironmentVariable("LDAP_HOST");
                var ldap_port = int.Parse(Environment.GetEnvironmentVariable("LDAP_PORT"));
                var ldap_base = Environment.GetEnvironmentVariable("LDAP_BASE");
                var ldap_id_attr = Environment.GetEnvironmentVariable("LDAP_ID_ATTR");
                var ldap_mail_attr = Environment.GetEnvironmentVariable("LDAP_MAIL_ATTR");
                var ldap_name_attr = Environment.GetEnvironmentVariable("LDAP_NAME_ATTR");
                var ldap_engname_attr = Environment.GetEnvironmentVariable("LDAP_ENGNAME_ATTR");
                var authenticator = new LdapAuthenticator(ldap_host, ldap_port, ldap_base, ldap_id_attr, entry => {
                    var attrs = entry.GetAttributeSet();
                    var email = attrs.GetAttribute(ldap_mail_attr).StringValue;
                    var xs = ldap_name_attr.Split(";");
                    string name = null;
                    if (xs.Length == 1)
                    {
                        name = attrs.GetAttribute(xs[0]).StringValue;
                    }
                    else
                    {
                        name = attrs.GetAttribute(xs[0], xs[1]).StringValue;
                    }
                    var ys = ldap_engname_attr.Split(";");
                    string ename = null;
                    if (ys.Length == 1)
                    {
                        ename = attrs.GetAttribute(ys[0]).StringValue;
                    }
                    else
                    {
                        ename = attrs.GetAttribute(ys[0], ys[1]).StringValue;
                    }
                    return (name, ename, email);
                });
                var (result, name, ename, email) = authenticator.Authenticate(user.Account, password);
                if (result && !user.IsLdapInitialized)
                {
                    user.DisplayName = name;
                    user.EnglishName = ename;
                    user.Email = email;
                    Update(user);
                }
                return result;
            }
            else
            {
                return user.EncryptedPassword == Encrypt(password);
            }
        }


        public string Encrypt(string rawPassword)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                var key = Conf.GetAppSecretKey();

                rijndael.IV = Encoding.UTF8.GetBytes(key.Substring(0, 16));
                rijndael.Key = Encoding.UTF8.GetBytes(key.Substring(16, 16));
                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(rawPassword);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }
    }
}
