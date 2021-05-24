using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text;
using System.Diagnostics;
using System.IO;
using Hugin.Models;

namespace Hugin.Services
{
    public class RepositoryHandleService
    {
        private readonly ConcurrentDictionary<string, object> locks;

        private readonly FilePathResolveService FilePathResolver;
        public RepositoryHandleService(FilePathResolveService filePathResolver)
        {
            FilePathResolver = filePathResolver;
            locks = new ConcurrentDictionary<string, object>();
        }

        public Repository GetLectureContentsRepository(string lectureOwner, string lectureName)
            => new Repository($"{FilePathResolver.GetLectureDirectoryPath(lectureOwner, lectureName)}/contents");
        public Repository GetLectureContentsRepository(Data.Lecture lecture)
            => new Repository($"{FilePathResolver.GetLectureDirectoryPath(lecture)}/contents");

        public Repository GetLectureUserDataRepository(string lectureOwner, string lectureName, string user)
            => new Repository($"{FilePathResolver.GetUserDirectoryPath(user)}/lecture-data/{lectureOwner}/{lectureName}");
        public Repository GetLectureUserDataRepository(Data.Lecture lecture, string user)
            => new Repository($"{FilePathResolver.GetUserDirectoryPath(user)}/lecture-data/{lecture.Owner.Account}/{lecture.Name}");


        public void CreateInitialLectureContentsRepository(Repository repository, string defaultBranch, Data.Lecture lecture, Data.User user, string message)
        {
            if (IsInitialized(repository))
            {
                throw new Exception($"The repository is not empty.");
            }

            var files = new Dictionary<string, string>();
            var sb = new StringBuilder();

            sb.AppendLine($"# {lecture.Name}");
            sb.AppendLine();
            sb.AppendLine(lecture.Description);
            files.Add("pages/index.md", sb.ToString());
            sb.Clear();

            files.Add("activities/.keep", "");
            files.Add("files/.keep", "");

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<Parameters>");
            sb.AppendLine("</Parameters>");
            files.Add("parameters.xml", sb.ToString());
            sb.Clear();

            SaveAndSync(repository, defaultBranch, files, null, message, user.DisplayName, user.Email);
        }

        public void CreateInitialLectureUserDataRepositoryIfNotExist(Repository repository, string branch, Data.Lecture lecture, Data.User user, string message)
        {
            if (!IsInitialized(repository))
            {
                Create(repository, "master");
                SaveTextAndSync(repository, branch, "home/.keep", "", message, user.DisplayName, user.Email, user.Uid + 1000);
            }

        }




        public void DoWithLock(Models.Repository repository, Action<Models.Repository> action)
        {
            object lk;
            lock (locks)
            {
                lk = locks.GetOrAdd(repository.Path, (key) => new object());            
            }
            lock(lk)
            {
                action.Invoke(repository);
            }
        }
        public T DoWithLock<T>(Models.Repository repository, Func<Models.Repository, T> action)
        {
            object lk;
            lock (locks)
            {
                lk = locks.GetOrAdd(repository.Path, (key) => new object());
            }
            lock (lk)
            {
                return action.Invoke(repository);
            }
        }


        public bool IsInitialized(Repository repository)
        {
            if(!Directory.Exists(repository.BaredFullPath))
            {
                return false;
            }
            try
            {
                return !string.IsNullOrWhiteSpace(executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"branch").Trim());
            }
            catch
            {
                return false;
            }
        }


        public void Create(Repository repository, string defaultBranch)
        {
            try
            {
                Directory.CreateDirectory(repository.BaredFullPath);
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"init --bare --shared .");
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to create new repository `{repository.Path}'.", e);
            }
        }
        public void Delete(Repository repository)
        {
            try
            {
                if (!Directory.Exists(repository.Path))
                {
                    throw new Exception($"The directory `{repository.Path}' does not exists.");
                }
                Directory.Delete(repository.Path, true);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to delete the repository `{repository.Path}'.", e);
            }
        }

        public void Move(Repository repository, string newPath)
        {
            try
            {
                if (!Directory.Exists(repository.Path))
                {
                    throw new Exception($"The directory `{repository.Path}' does not exists.");
                }
                if (Directory.Exists(newPath))
                {
                    throw new Exception($"The directory `{newPath}' already exists.");
                }
                Directory.Move(repository.Path, newPath);
                repository.Path = newPath;
                ResetRemoteUrl(repository);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to move the repository `{repository.Path}' to `{newPath}'.", e);
            }
        }

        #region for bared repository



        public string Pack(Repository repository, PackService pack_service)
        {
            var service = pack_service switch
            {
                PackService.GitReceivePack => "git-receive-pack",
                _ => "git-upload-pack",
            };
            return DoWithLock(repository, r =>
            {
                var output = executeWithExceptionProcessing(r.BaredFullPath, service, $"--advertise-refs .");
                var result = System.Text.Encoding.UTF8.GetString(output);
                var head = $"# service={service}\n0000";
                return $"{head.Length.ToString("x04")}{head}{result}";
            });
        }

        public async Task<(bool, byte[])> Pack(Repository repository, PackService pack_service, Stream body)
        {
            var service = pack_service switch
            {
                PackService.GitReceivePack => "git-receive-pack",
                _ => "git-upload-pack",
            };
            return await DoWithLock(repository, async r =>
            {
                using (var msr = new MemoryStream())
                {
                    await body.CopyToAsync(msr);
                    var input = msr.ToArray();
                    var noflash = input.Length == 4 && input[0] == 48 && input[1] == 48 && input[2] == 48 && input[3] == 48;
                    return (noflash, executeWithExceptionProcessing(r.BaredFullPath, service, $"--stateless-rpc .", input));
                }
            });

        }



        public IEnumerable<string> GetBranches(Repository repository)
        {
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, "branch --format=\"%(refname:short)\"")
                .Trim().Split().Where(x => !string.IsNullOrWhiteSpace(x));
        }
        public IEnumerable<string> GetFileNames(Repository repository, string path, string rivision)
        {
            path = !string.IsNullOrWhiteSpace(path) ? $"-- \"{path}\"" : "";
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"ls-tree --full-tree -r -z --name-only \"{rivision}\" {path}")
                .Split('\0').Select(x => x.Trim('"')).Where(x => !string.IsNullOrWhiteSpace(x));
        }
        public CommitInfo ReadCommitInfo(Repository repository, string path, string rivision)
        {
            path = !string.IsNullOrWhiteSpace(path) ? $"-- \"{path}\"" : "";

            var message = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"%s\" \"{rivision}\" {path}").Trim();
            var hashes = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"%H %h\" \"{rivision}\" -- {path}").Trim().Split(" ");
            var authorName = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"%an\" \"{rivision}\" -- {path}").Trim();
            var authorEmail = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"%ae\" \"{rivision}\" -- {path}").Trim();
            var datestring = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"%ad\" \"{rivision}\" -- {path}").Trim();

            DateTime.TryParseExact(datestring,
                "ddd MMM d HH:mm:ss yyyy K",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date);
            var shortHash = hashes.Count() >= 2 ? hashes[1] : "";
            return new CommitInfo()
            {
                Message = message,
                Hash = hashes[0],
                ShortHash = shortHash,
                AuthorName = authorName,
                AuthorEmail = authorEmail,
                Date = date
            };
        }

        public bool Exists(Repository repository, string path, string rivision)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(GetHashOfLatestCommit(repository, path, rivision));
            }
            catch
            {
                return false;
            }
        }

        public string GetHashOfLatestCommit(Repository repository, string path, string rivision, string grepText = "")
        {
            path = !string.IsNullOrWhiteSpace(path) ? $"-- \"{path}\"" : "";
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log --pretty=\"%H\" --grep=\"{grepText}\" -1 {rivision} {path}").Trim();
        }

        public IEnumerable<string> GetHashesOfAllCommits(Repository repository, string path, string rivision, string grepText)
        {
            path = !string.IsNullOrWhiteSpace(path) ? $"-- \"{path}\"" : "";
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log --pretty=\"%H\" --grep=\"{grepText}\" {rivision} {path}")
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        }

        public bool TypeCheck(Repository repository, string path, string rivision)
        {
            path = !string.IsNullOrWhiteSpace(path) ? $"-- \"{path}\"" : "";
            var chk = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"\" \"{rivision}\" --numstat {path}")[0];
            return chk != '-';
        }
        public (byte[], bool) ReadFileWithTypeCheck(Repository repository, string path, string rivision)
        {
            var data = executeGitCommandWithExceptionProcessing(repository.BaredFullPath, $"show \"{rivision}\":\"{path}\"");
            path = !string.IsNullOrWhiteSpace(path) ? $"-- \"{path}\"" : "";
            var chk = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"log -1 --pretty=\"\" \"{rivision}\" --numstat {path}")[0];
            return (data, chk != '-');
        }
        public string ReadTextFile(Repository repository, string path, string rivision)
        {
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.BaredFullPath, $"show \"{rivision}\":\"{path}\"");
        }
        #endregion


        #region for non-bared repository

        public string SaveTextAndSync(Repository repository, string branch, string path, string text, string message, string author, string email, int ownerId = -1)
        {
            var dict = new Dictionary<string, string>();
            dict.Add(path, text);
            return SaveAndSync(repository, branch, dict, null, message, author, email, ownerId);
        }
        public string SaveBinaryAndSync(Repository repository, string branch, string path, byte[] data, string message, string author, string email, int ownerId = -1)
        {
            var dict = new Dictionary<string, byte[]>();
            dict.Add(path, data);
            return SaveAndSync(repository, branch, null, dict, message, author, email, ownerId);
        }
        public string SaveAndSync(Repository repository, string branch, IDictionary<string, string> textfiles, IDictionary<string, byte[]> binaryfiles, string message, string author, string email, int ownerId = -1)
        {
            var files = new List<string>();

            if (GetBranches(repository).Contains(branch))
            {
                PullFromBare(repository, branch);
            }
            else
            {
                if (Directory.Exists(repository.GetNonBaredFullPath(branch)))
                {
                    Directory.Delete(repository.GetNonBaredFullPath(branch), true);
                }
                Directory.CreateDirectory(repository.GetNonBaredFullPath(branch));
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), "init .");
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"remote add origin {repository.BaredFullPath}");
            }

            if(textfiles != null)
            {
                foreach (var path in textfiles.Keys)
                {
                    var file = $"{repository.GetNonBaredFullPath(branch)}{path}";
                    var fileInfo = new FileInfo(file);
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                        if (ownerId > 0)
                        {
                            Process.Start("chown", $"-R {ownerId}:{ownerId} {repository.GetNonBaredFullPath(branch)}").WaitForExit();
                        }
                    }
                    using (var w = new StreamWriter(file))
                    {
                        w.Write(textfiles[path]);
                    }
                    if (ownerId > 0)
                    {
                        Process.Start("chown", $" {ownerId}:{ownerId} {file}").WaitForExit();
                    }
                    files.Add(file);
                }
            }
            if (binaryfiles != null)
            {
                foreach (var path in binaryfiles.Keys)
                {
                    var file = $"{repository.GetNonBaredFullPath(branch)}{path}";
                    var fileInfo = new FileInfo(file);
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                        if (ownerId > 0)
                        {
                            Process.Start("chown", $"-R {ownerId}:{ownerId} {repository.GetNonBaredFullPath(branch)}").WaitForExit();
                        }
                    }
                    using (var w = new FileStream(file, FileMode.Create, FileAccess.Write))
                    {
                        w.Write(binaryfiles[path]);
                    }
                    if (ownerId > 0)
                    {
                        Process.Start("chown", $" {ownerId}:{ownerId} {file}").WaitForExit();
                    }
                    files.Add(file);
                }
            }
            
            var result = Commit(repository, branch, files, message, author, email);
            PushToBare(repository, branch);
            return result;
        }



        public string RemoveAndSync(Repository repository, string branch, IEnumerable<string> paths, string message, string author, string email, int ownerId = -1)
        {
            var files = new List<string>();

            if (!GetBranches(repository).Contains(branch))
            {
                throw new FileNotFoundException();
            }

            if (paths != null)
            {
                foreach (var path in paths)
                {
                    executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"rm -r {path}");
                }
            }

            var result = Commit(repository, branch, files, message, author, email);
            PushToBare(repository, branch);
            return result;
        }

        public string CommitAll(Repository repository, string branch, string message, string author, string email)
        {
            var xs = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), "add .");
            var ys = executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"-c user.name=\"{author}\" -c user.email=\"{email}\" commit --allow-empty --file=-", Encoding.UTF8.GetBytes(message));
            return ys;
        }
        public string Commit(Repository repository, string branch, IEnumerable<string> files, string message, string author, string email)
        {
            foreach (var file in files)
            {
                executeGitCommandWithOutputEncoding(repository.GetNonBaredFullPath(branch), $"add {file}");
            }
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"-c user.name=\"{author}\" -c user.email=\"{email}\" commit --allow-empty --file=-", Encoding.UTF8.GetBytes(message));

        }

        public string PushToBare(Repository repository, string branch)
        {
            return executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"push origin {branch}");
        }

        public void PullFromBare(Repository repository, string branch)
        {
            if(!Directory.Exists(repository.GetNonBaredFullPath(branch)))
            {
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.Path, $"clone -b {branch} {repository.BaredFullPath} {repository.GetNonBaredFullPath(branch)}");
            }
            else
            {
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), "clean -fdx");
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"fetch origin {branch}");
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), $"reset --hard origin/{branch}");
                executeGitCommandWithOutputEncodingAndExceptionProcessing(repository.GetNonBaredFullPath(branch), "clean -fdx");
            }
        }
        public void ResetRemoteUrl(Repository repository)
        {
            foreach(var p in repository.GetNonBaredFullPaths())
            {
                executeGitCommandWithOutputEncodingAndExceptionProcessing(p, $"remote set-url origin {repository.BaredFullPath}");
            }
        }
        #endregion

        #region private methods

        private string executeGitCommandWithOutputEncodingAndExceptionProcessing(string working_dir, string arguments, byte[] input = null)
        {
            var (result, error, code) = execute(working_dir, "git", arguments, input);
            if (code != 0)
            {
                throw new Exception($"Git error: {Encoding.UTF8.GetString(error)}.");
            }
            return Encoding.UTF8.GetString(result);
        }
        private string executeGitCommandWithOutputEncoding(string working_dir, string arguments, byte[] input = null)
        {
            var (result, error, code) = execute(working_dir, "git", arguments, input);
            return Encoding.UTF8.GetString(result);
        }
        private byte[] executeGitCommandWithExceptionProcessing(string working_dir, string arguments, byte[] input = null)
        {
            return executeWithExceptionProcessing(working_dir, "git", arguments, input);
        }
        private byte[] executeWithExceptionProcessing(string working_dir, string program, string arguments, byte[] input = null)
        {
            var (result, error, code) = execute(working_dir, program, arguments, input);
            if (code != 0)
            {
                throw new Exception($"Git error: {Encoding.UTF8.GetString(error)}.");
            }
            return result;
        }
        private (byte[], byte[], int) execute(string working_dir, string program, string arguments, byte[] input = null)
        {
            var psinfo = new ProcessStartInfo();
            psinfo.FileName = program;
            psinfo.Arguments = arguments;
            psinfo.WorkingDirectory = working_dir;
            psinfo.CreateNoWindow = true;
            psinfo.UseShellExecute = false;
            psinfo.RedirectStandardInput = input != null;
            psinfo.RedirectStandardOutput = true;
            psinfo.RedirectStandardError = true;

            var proc = Process.Start(psinfo);
            if (input != null)
            {
                proc.StandardInput.BaseStream.Write(input, 0, input.Length);
                proc.StandardInput.Close();
            }

            var msw1 = new MemoryStream();
            proc.StandardOutput.BaseStream.CopyTo(msw1);
            proc.StandardOutput.Close();
            var buff1 = msw1.ToArray();
            msw1.Close();

            var msw2 = new MemoryStream();
            proc.StandardError.BaseStream.CopyTo(msw2);
            proc.StandardError.Close();
            var buff2 = msw2.ToArray();
            msw2.Close();

            proc.WaitForExit();
            int code = proc.ExitCode;
            proc.Close();
            return (buff1, buff2, code);
        }
        #endregion
    }
}
