using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class FilePathResolveService
    {
        private readonly ApplicationConfigurationService Conf;
        public FilePathResolveService(ApplicationConfigurationService conf)
        {
            Conf = conf;
        }

        public string UsersDirectoryPath { get => $"{Conf.GetAppDataPath()}/users"; }
        public string GetUserDirectoryPath(string account) => $"{UsersDirectoryPath}/{account}";


        public string GetLectureDirectoryPath(string lectureOwner, string lectureName) => $"{GetUserDirectoryPath(lectureOwner)}/lectures/{lectureName}";
        public string GetLectureDirectoryPath(Data.Lecture lecture) => GetLectureDirectoryPath(lecture.Owner.Account, lecture.Name);


        public string GetLectureUserDataDirectoryPath(string lectureOwner, string lectureName, string user, string branch) =>
            $"{GetUserDirectoryPath(user)}/lecture-data/{lectureOwner}/{lectureName}/nobared/{branch}";
        public string GetLectureUserDataDirectoryPath(Data.Lecture lecture, string user, string branch) =>
            $"{GetUserDirectoryPath(user)}/lecture-data/{lecture.Owner.Account}/{lecture.Name}/nobared/{branch}";


        public string GetSandboxDirectoryPath(string lectureOwner, string lectureName, string sandboxName) =>
            $"{GetLectureDirectoryPath(lectureOwner, lectureName)}/sandboxes/{sandboxName}";
        public string GetSandboxDirectoryPath(Data.Lecture lecture, string sandboxName) =>
            $"{GetLectureDirectoryPath(lecture)}/sandboxes/{sandboxName}";
        public string GetSandboxDirectoryPath(Data.Sandbox sandbox) =>
            $"{GetLectureDirectoryPath(sandbox.Lecture)}/sandboxes/{sandbox.Name}";


        public string GetLectureContentsGitApiURL(string lectureOwner, string lectureName) =>
            $"{Conf.GetAppURL()}/Git/LectureContents/{lectureOwner}/{lectureName}.git";
        public string GetLectureUserDataGitApiURL(string lectureOwner, string lectureName, string userAccount) =>
            $"{Conf.GetAppURL()}/Git/LectureUserData/{lectureOwner}/{lectureName}/{userAccount}.git";

    }
}
