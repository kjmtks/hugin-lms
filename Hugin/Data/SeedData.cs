using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;

namespace Hugin.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(DatabaseContext context, UserHandleService userHandler, LectureHandleService lectureHander, ResourceHubHandleService resourceHubHandle)
        {
            if(await context.Users.AnyAsync())
            {
                // DB has been seeded  
                return;
            }

            var admin = userHandler.AddNew(new User
            {
                Account = "admin",
                DisplayName = "管理者",
                EnglishName = "Administrator",
                Email = "admin@localhost",
                RawPassword = "password",
                IsAdmin = true,
                IsTeacher = true,
                IsLdapUser = false
            });
            var lecture = lectureHander.AddNew(new Lecture
            {
                OwnerId = admin.Id,
                Name = "demo",
                Subject = "Demo Lecture",
                Description = "for Demo",
                DefaultBranch = "master",
                IsActived = true,
                Opened = true
            });


            foreach (var i in Enumerable.Range(1, 3))
            {
                var user = userHandler.AddNew(new User
                {
                    Account = string.Format("test{0:000}", i),
                    DisplayName = string.Format("テストユーザー #{0:000}", i),
                    EnglishName = string.Format("Test User #{0:000}", i),
                    Email = string.Format("test{0:000}@localhost", i),
                    RawPassword = "password",
                    IsLdapUser = false
                });
                var rel = new LectureUserRelationship
                {
                    LectureId = lecture.Id,
                    UserId = user.Id,
                    Role = LectureUserRelationship.LectureRole.Student
                };
                context.LectureUserRelationships.Add(rel);
                context.SaveChanges();
            }

            //resourceHubHandle.AddNew(new ResourceHub 
            //{
            //    YamlURL = "https://raw.githubusercontent.com/kjmtks/hugin-hub/main/hub.yaml"
            //});
        }
    }
}
