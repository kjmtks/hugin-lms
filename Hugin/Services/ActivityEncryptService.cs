using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Services
{
    public class ActivityEncryptService
    {

        private readonly ApplicationConfigurationService Conf;
        public ActivityEncryptService(ApplicationConfigurationService conf)
        {
            Conf = conf;
        }

        public class SerializedActivityProfile
        {
            public int Number { get; set; }
            public string UserAccount { get; set; }
            public string LectureOwnerAccount { get; set; }
            public string LectureName { get; set; }
            public string ActivityName { get; set; }
            public string ActivityRef { get; set; }
            public string[][] Parameters { get; set; }
            public string PagePath { get; set; }
            public string Rivision { get; set; }

            public bool CanUseSubmit { get; set; }
            public bool CanUseAnswer { get; set; }
        }

        public Models.ActivityProfile Decrypt(string encrypted_string)
        {
            var decrypted_string = NETCore.Encrypt.EncryptProvider.AESDecrypt(encrypted_string, Conf.GetEncryptKey());
            SerializedActivityProfile sprof;

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    sw.Write(decrypted_string);
                }
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializedActivityProfile));
                sprof = (SerializedActivityProfile)serializer.Deserialize(new MemoryStream(ms.ToArray()));
            }

            var prof = new Models.ActivityProfile
            {
                Number = sprof.Number,
                UserAccount = sprof.UserAccount,
                LectureOwnerAccount = sprof.LectureOwnerAccount,
                LectureName = sprof.LectureName,
                ActivityName = sprof.ActivityName,
                ActivityRef = sprof.ActivityRef,
                Parameters = new Dictionary<string, string>(),
                PagePath = sprof.PagePath,
                Rivision = sprof.Rivision,
                CanUseSubmit = sprof.CanUseSubmit,
                CanUseAnswer = sprof.CanUseAnswer,
            };
            foreach (var xs in sprof.Parameters)
            {
                prof.Parameters.Add(xs[0], xs[1]);
            }
            return prof;
        }


        public string Encrypt(Models.ActivityProfile prof)
        {
            var sprof = new SerializedActivityProfile()
            {
                Number = prof.Number,
                UserAccount = prof.UserAccount,
                LectureOwnerAccount = prof.LectureOwnerAccount,
                LectureName = prof.LectureName,
                ActivityName = prof.ActivityName,
                ActivityRef = prof.ActivityRef, 
                Parameters = prof.Parameters.Select(x => new string[] { x.Key, x.Value }).ToArray(),
                PagePath = prof.PagePath,
                Rivision = prof.Rivision,
                CanUseSubmit = prof.CanUseSubmit,
                CanUseAnswer = prof.CanUseAnswer,
            };

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializedActivityProfile));
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    serializer.Serialize(ms, sprof);
                }
                return NETCore.Encrypt.EncryptProvider.AESEncrypt(System.Text.Encoding.UTF8.GetString(ms.ToArray()), Conf.GetEncryptKey());
            }
        }
    }
}
