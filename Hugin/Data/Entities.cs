using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Data
{
    public interface IEntity
    {
        int Id { get; }
    }
    public class User : IEntity
    {
        [Key, Required]
        public int Id { get; set; }

        [Required]
        public int Uid { get; set; }

        [RegularExpression(@"^[_A-Za-z][_A-Za-z0-9-]+$")]
        [Required, StringLength(32)]
        public string Account { get; set; }

        [Required, StringLength(64), RegularExpression("^[^\\\\\\\"]+$")]
        public string DisplayName { get; set; }

        [Required, StringLength(64), RegularExpression("^[^\\\\\\\"]+$")]
        public string EnglishName { get; set; }

        [NotMapped]
        public string RawPassword { get; set; }
        public string EncryptedPassword { get; set; }

        public bool IsAdmin { get; set; }
        public bool IsTeacher { get; set; }
        public bool IsLdapUser { get; set; }
        public bool IsLdapInitialized { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public virtual ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        public virtual ICollection<LectureUserRelationship> LectureUserRelationships { get; set; } = new List<LectureUserRelationship>();
    }

    public class Lecture : IEntity
    {
        [Key, Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Subject { get; set; }
        public string Description { get; set; }
        public bool Opened { get; set; }
        public bool IsActived { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; }

        [Required]
        public string DefaultBranch { get; set; }

        public virtual ICollection<LectureUserRelationship> LectureUserRelationships { get; set; } = new List<LectureUserRelationship>();
        public virtual ICollection<Sandbox> Sandboxes { get; set; } = new List<Sandbox>();

        [NotMapped]
        public string RepositoryCloneFrom { get; set; }
    }
    public class LectureUserRelationship
    {
        [Key, Required, ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Key, Required, ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }

        public LectureRole Role { get; set; }

        [Flags]
        public enum LectureRole
        {
            Banned = 0x0000,

            CanShowPage = 0x0001,
            CanSubmitActivity = 0x0002,
            CanAnswerActivity = 0x0004,
            CanShowSubmission = 0x0008,
            CanMarkSubmission = 0x0010,
            CanReadContentsRepository = 0x0020,
            CanWriteContentsRepository = 0x0040,
            CanEditSandbox = 0x0080,

            Student   = CanShowPage | CanSubmitActivity,
            Observer  = CanShowPage | CanAnswerActivity | CanShowSubmission | CanReadContentsRepository,
            Assistant = CanShowPage | CanSubmitActivity | CanAnswerActivity | CanShowSubmission | CanMarkSubmission | CanReadContentsRepository,
            Editor    = CanShowPage | CanSubmitActivity | CanAnswerActivity | CanReadContentsRepository | CanWriteContentsRepository | CanEditSandbox,
            Lecurer   = CanShowPage | CanSubmitActivity | CanAnswerActivity | CanShowSubmission | CanMarkSubmission | CanReadContentsRepository | CanWriteContentsRepository | CanEditSandbox,
            Test      = 0xffff
        }
    }

    public class Sandbox : IEntity
    {
        [Key, Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }

        public SandboxState State { get; set; }

        public enum SandboxState
        {
            Uninstalled = 0,
            Installing = 1,
            Installed = 2,
        }
    }

    public class ActivityAction : IEntity
    {
        [Key, Required]
        public int Id { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }
        [Required]
        public string ActivityName { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }
        
        public Models.ActivityActionTypes ActivityActionType { get; set; }

        public string Tags { get; set; }
        public string Page { get; set; }
        public string Summary { get; set; }
        public DateTime RequestedAt { get; set; }
        public string AdditionalFiles { get; set; }
    }

    public class Submission : IEntity
    {
        [Key, Required]
        public int Id { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        public string ActivityName { get; set; }

        public string Tags { get; set; }
        public string Page { get; set; }

        [Required]
        public string Hash { get; set; }
        public string SubmittedFiles { get; set; }
        public string SubumitComment { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? Deadline { get; set; }

        public SubmissionState State { get; set; }
        public string Grade { get; set; }
        public string FeedbackComment { get; set; }

        [ForeignKey("MarkerUser")]
        public int? MarkerUserId { get; set; }
        public virtual User MarkerUser { get; set; }
        public DateTime? MarkedAt { get; set; }

        public DateTime? ResubmitDeadline { get; set; }

        public int Count { get; set; }
        public enum SubmissionState
        {
            Deleted = -1,
            Empty = 0, 
            Submitted = 1,
            RequiringResubmit = 2,
            AcceptingResubmit = 3,
            Confirmed = 4,
            Disqualified = 5
        }

        public int NumOfSaves { get; set; }
        public int NumOfRuns { get; set; }
        public int NumOfValidateRejects { get; set; }
        public int NumOfValidateAccepts { get; set; }
    }

    public class ActivityMessage : IEntity
    {
        [Key, Required]
        public int Id { get; set; }
        
        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }

        [ForeignKey("ToUser")]
        public int ToUserId { get; set; }
        public virtual User ToUser { get; set; }

        public bool ToAll { get; set; }

        [Required]
        public string ActivityName { get; set; }

        [ForeignKey("Author")]
        public int AuthorId { get; set; }
        public virtual User Author { get; set; }
        public string Body { get; set; }
        public DateTime SendAt { get; set; }
    }

    public class ResourceHub : IEntity
    {
        [Key, Required]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public string YamlURL { get; set; }
    }
}
