using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Rendering;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Hugin.Services;
using System.Text;

namespace Hugin.Models
{

    [Serializable]
    public partial class Activity
    {
        [XmlAttribute]
        public string Version { get; set; }
        public string Sandbox { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }

        [XmlElement("Description")]
        public ActivityDescription Description { get; set; }

        [XmlIgnore]
        public DateTime? Deadline { get; set; }

        [XmlElement("Deadline")]
        public string _Deadline 
        {
            get
            {
                
                return Deadline?.ToString("yyyy/MM/dd HH:mm:ss");
            }
            set
            {
                if (DateTime.TryParse(value, out var dt))
                {
                    Deadline = dt;
                }
                else
                {
                    Deadline = null;
                }
            }
        }
        

        public string Directory { get; set; }
        public string Tags { get; set; }
        public ActivityOptions Options { get; set; }
        public ActivityFiles Files { get; set; }
        public ActivityRunners Runners { get; set; }
        public ActivityLimits Limits { get; set; }
        public ActivityValidations Validations { get; set; }

        public bool UseSave()
        {
            return this.Options.UseSave;
        }
        public bool UseReset()
        {
            return this.Options.UseReset;
        }
        public bool UseDiscard()
        {
            return this.Options.UseDiscard;
        }
        public bool UseValidate()
        {
            return this.Validations != null && this.Validations.Child != null;
        }
        public bool UseSubmit()
        {
            return this.Files.Children.Any(f => f.Submit);
        }
        public bool UseAnswer()
        {
            return this.Files.Children.Any(f => f.HasAnswer());
        }

        public IEnumerable<string> GetFilePaths()
        {
            return Files.Children.Select(x => ToPath(x.Name));
        }
        public IEnumerable<string> GetSubmittedFilePaths()
        {
            return Files.Children.Where(x => x.Submit).Select(x => ToPath(x.Name));
        }

        public string ToPath(string filename)
        {
            var dir = Directory?.TrimEnd('/') ?? "";
            if (!string.IsNullOrWhiteSpace(dir))
            {
                dir = $"{dir}/";
            }
            return $"{dir}{filename}";
        }
    }

    [Serializable]
    public partial class ActivityDescription
    {
        [XmlAttribute("UseMarkdown")]
        public bool UseMarkdown { get; set; } = false;
        [XmlText]
        public string Text { get; set; }
    }

    [Serializable]
    public partial class ActivityRunners
    {
        [XmlElement("Runner")]
        public ActivityRunner[] Runners { get; set; }
    }

    [Serializable]
    public partial class ActivityRunner
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlAttribute("Subject")]
        public string Subject { get; set; }
        [XmlAttribute("Icon")]
        public string Icon { get; set; }

        [XmlAttribute("Auxiliary")]
        public bool Auxiliary { get; set; } = true;
        [XmlText]
        public string Commands { get; set; }
    }

    [Serializable]
    public partial class ActivityOptions
    {
        public bool UseMarkdown { get; set; } = true;
        public bool UseStdout { get; set; } = true;
        public bool UseStderr { get; set; } = true;
        public bool UseSave { get; set; } = true;
        public bool UseReset { get; set; } = true;
        public bool UseDiscard { get; set; } = true;
        public bool CanSubmitBeforeAccept { get; set; } = true;
        public bool CanSubmitBeforeRun { get; set; } = true;
        public bool CanValidateBeforeRun { get; set; } = true;
        public bool CanSubmitAfterDeadline { get; set; } = false;
        public string DefaultSubmitStatus { get; set; } = "submitted";
    }

    [Serializable]
    public partial class ActivityLimits
    {
        public uint CpuTime { get; set; }
        public string Memory { get; set; }
        public uint StdoutLength { get; set; }
        public uint StderrLength { get; set; }
        public uint Pids { get; set; }
    }


    public interface IActivityFile
    {
        string Name { get; }
        string Label { get; }
        bool Submit { get; }
        bool HasDefault();
        bool HasAnswer();
        string Default { get; }
        string Answer { get; }
    }

    [Serializable]
    public partial class ActivityFiles
    {
        [XmlElement("Blockly", typeof(ActivityFilesBlockly))]
        [XmlElement("Code", typeof(ActivityFilesCode))]
        [XmlElement("Text", typeof(ActivityFilesText))]
        [XmlElement("String", typeof(ActivityFilesString))]
        [XmlElement("Upload", typeof(ActivityFilesUpload))]
        [XmlElement("Form", typeof(ActivityFilesForm))]
        public object[] Files { get; set; }
        public IEnumerable<IActivityFile> Children { get { return this.Files.Cast<IActivityFile>(); } }
    }

    [Serializable]
    public partial class ActivityFilesBlockly : IActivityFile
    {
        public string Default { get; set; }

        public string Answer { get; set; }

        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }
        public bool HasDefault() { return !string.IsNullOrWhiteSpace(Default); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string Height { get; set; } = "480px";

        [XmlAttribute]
        public string CodeFile { get; set; }

        [XmlAttribute]
        public bool UseCodeViewer { get; set; } = true;

        public string Configure { get; set; }

        public string GetToolboxHtml()
        {
            if(Toolbox == null)
            {
                return "";
            }
            var sb = new StringBuilder();
            foreach(var t in Toolbox as System.Xml.XmlNode[])
            {
                sb.AppendLine(t.OuterXml);
            }
            return sb.ToString();
        }

        [XmlElement]
        public object Toolbox { get; set; }

        [XmlElement("Block")]
        public ActivityFilesBlocklyBlock[] Blocks { get; set; }

        [XmlAttribute]
        public bool ReadOnly { get; set; } = false;
        [XmlAttribute]
        public bool Submit { get; set; } = false;
    }


    [Serializable]
    public partial class ActivityFilesBlocklyBlock
    {
        public string Definition { get; set; }
        public string Generator { get; set; }
    }
    

    [Serializable]
    public partial class ActivityFilesBlockToolbox
    {
        [XmlText]
        public string Body { get; set; }
    }
    [Serializable]
    public partial class ActivityFilesCode : IActivityFile
    {
        public string Default { get; set; }

        public string Answer { get; set; }

        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }
        public bool HasDefault() { return !string.IsNullOrWhiteSpace(Default); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string Language { get; set; } = "plaintext";
        [XmlAttribute]
        public int Maxlength { get; set; } = -1;
        [XmlAttribute]
        public bool ReadOnly { get; set; } = false;
        [XmlAttribute]
        public bool Submit { get; set; } = false;
    }

    [Serializable]
    public partial class ActivityFilesText : IActivityFile
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }
        public bool HasDefault() { return !string.IsNullOrWhiteSpace(Default); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public int Maxlength { get; set; } = -1;
        [XmlAttribute]
        public bool ReadOnly { get; set; } = false;
        [XmlAttribute]
        public bool Submit { get; set; } = false;
    }

    [Serializable]
    public partial class ActivityFilesString : IActivityFile
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }
        public bool HasDefault() { return !string.IsNullOrWhiteSpace(Default); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public int Maxlength { get; set; } = -1;
        [XmlAttribute]
        public bool ReadOnly { get; set; } = false;
        [XmlAttribute]
        public bool Submit { get; set; } = false;
    }

    [Serializable]
    public partial class ActivityFilesUpload : IActivityFile
    {
        public bool HasAnswer() { return false; }
        public bool HasDefault() { return false; }

        public string Answer { get { return null; } }
        public string Default { get { return null; } }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string Accept { get; set; }
        [XmlAttribute]
        public ulong Maxsize { get; set; } = 5 * 1024 * 1024;
        [XmlAttribute]
        public bool Submit { get; set; } = false;
    }




    [Serializable]
    public partial class ActivityFilesForm : IActivityFile
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public bool Submit { get; set; }

        [XmlElement("Text", typeof(ActivityFilesFormText))]
        [XmlElement("Checkbox", typeof(ActivityFilesFormCheckbox))]
        [XmlElement("Radio", typeof(ActivityFilesFormRadio))]
        [XmlElement("Select", typeof(ActivityFilesFormSelect))]
        [XmlElement("String", typeof(ActivityFilesFormString))]
        [XmlElement("Textarea", typeof(ActivityFilesFormTextarea))]
        public object[] Forms { get; set; }
        public IEnumerable<IActivityFilesFormInput> Children { get { return this.Forms.Cast<IActivityFilesFormInput>(); } }

        public bool HasAnswer() { return Children.Any(x => !string.IsNullOrWhiteSpace(x.GetAnswer())); }
        public bool HasDefault() { return Children.Any(x => !string.IsNullOrWhiteSpace(x.GetDefault())); }

        public string Answer {
            get
            {
                var dict = new Dictionary<string, string>();
                foreach(var x in Children.Where(x => !(x is ActivityFilesFormText)))
                {
                    dict[x.Name] = x.GetAnswer();
                }
                return System.Text.Json.JsonSerializer.Serialize(dict);
            }
        }
        public string Default
        {
            get
            {
                var dict = new Dictionary<string, string>();
                foreach (var x in Children.Where(x => !(x is ActivityFilesFormText)))
                {
                    dict[x.Name] = x.GetDefault();
                }
                return System.Text.Json.JsonSerializer.Serialize(dict);
            }
        }

    }

    public interface IActivityFilesFormInput
    {
        string Name { get; }
        string GetAnswer();
        string GetDefault();
    }



    [Serializable]
    public partial class ActivityFilesFormText : IActivityFilesFormInput
    {
        [XmlText]
        public string Text { get; set; }
        [XmlAttribute]

        public string Name { get { return null; } }
        public string GetAnswer()
        {
            return null;
        }

        public string GetDefault()
        {
            return null;
        }
    }
    [Serializable]
    public partial class ActivityFilesFormCheckbox : IActivityFilesFormInput
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string True { get; set; }
        [XmlAttribute]
        public string False { get; set; }
        [XmlAttribute]
        public bool Default { get; set; }
        [XmlAttribute]
        public bool Answer { get; set; }
        [XmlText]
        public string Label { get; set; }

        public string GetAnswer()
        {
            return this.Answer ? this.True : this.False;
        }

        public string GetDefault()
        {
            return this.Default ? this.True : this.False;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormRadio : IActivityFilesFormInput
    {
        [XmlElement("Option")]
        public ActivityFilesFormRadioOption[] Options { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Label { get; set; }
        public string GetAnswer()
        {
            return this.Options.FirstOrDefault(opt => opt.Answer)?.Value;
        }

        public string GetDefault()
        {
            return this.Options.FirstOrDefault(opt => opt.Default)?.Value;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormRadioOption
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }
        [XmlAttribute]
        public bool Default { get; set; } = false;
        [XmlAttribute]
        public bool Answer { get; set; } = false;
        [XmlText]
        public string Label { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesFormSelect : IActivityFilesFormInput
    {
        [XmlElement("Option")]
        public ActivityFilesFormSelectOption[] Options { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Label { get; set; }

        public string GetAnswer()
        {
            return this.Options.FirstOrDefault(opt => opt.Answer)?.Value;
        }

        public string GetDefault()
        {
            return this.Options.FirstOrDefault(opt => opt.Default)?.Value;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormSelectOption
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }
        [XmlAttribute]
        public bool Default { get; set; } = false;
        [XmlAttribute]
        public bool Answer { get; set; } = false;
        [XmlText]
        public string Label { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesFormString : IActivityFilesFormInput
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public uint Maxlength { get; set; } = 1000;

        public string GetAnswer()
        {
            return this.Answer;
        }
        public string GetDefault()
        {
            return this.Default;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormTextarea : IActivityFilesFormInput
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }

        [XmlAttribute]
        public uint Maxlength { get; set; } = 100000;
        [XmlAttribute]
        public uint Rows { get; set; } = 4;

        public string GetAnswer()
        {
            return this.Answer;
        }
        public string GetDefault()
        {
            return this.Default;
        }
    }





    [Serializable]
    public partial class ActivityValidations
    {
        [XmlElement("Conjunction", typeof(Conjunction)), XmlElement("Disjunction", typeof(Disjunction)), XmlElement("Negation", typeof(Negation)), XmlElement("Validation", typeof(Validation))]
        public object Child { get; set; }
    }

    [Serializable]
    public partial class Validation : IValidatable
    {
        public string Run { get; set; }
        public string Answer { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
        public Task<bool> ValidateAsync(Validator validator)
        {
            return validator(this);
        }
    }

    [Serializable]
    public partial class Negation : IValidatable
    {
        [XmlElement("Conjunction", typeof(Conjunction)), XmlElement("Disjunction", typeof(Disjunction)), XmlElement("Negation", typeof(Negation)), XmlElement("Validation", typeof(Validation))]
        public object Child { get; set; }

        public async Task<bool> ValidateAsync(Validator validator)
        {
            var c = this.Child as IValidatable;
            if (c != null)
            {
                return !(await c.ValidateAsync(validator));
            }
            else
            {
                // throw new FormatException("Negation tag should have a child.");
                return false;
            }
        }
    }

    [Serializable]
    public partial class Conjunction : IValidatable
    {
        [XmlElement("Validation")]
        public Validation[] Validations { get; set; }

        [XmlElement("Negation")]
        public Negation[] NegativeChildren { get; set; }
        [XmlElement("Conjunction")]
        public Conjunction[] ConjunctiveChildren { get; set; }
        [XmlElement("Disjunction")]
        public Disjunction[] DisjunctiveChildren { get; set; }
        public async Task<bool> ValidateAsync(Validator validator)
        {
            if(Validations != null)
            {
                foreach (var c in Validations)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            if (NegativeChildren != null)
            {
                foreach (var c in NegativeChildren)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            if (ConjunctiveChildren != null)
            {
                foreach (var c in ConjunctiveChildren)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            if (DisjunctiveChildren != null)
            {
                foreach (var c in DisjunctiveChildren)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            return true;
        }
    }

    [Serializable]
    public partial class Disjunction : IValidatable
    {
        [XmlElement("Validation")]
        public Validation[] Validations { get; set; }

        [XmlElement("Negation")]
        public Negation[] NegativeChildren { get; set; }
        [XmlElement("Conjunction")]
        public Conjunction[] ConjunctiveChildren { get; set; }
        [XmlElement("Disjunction")]
        public Disjunction[] DisjunctiveChildren { get; set; }
        public async Task<bool> ValidateAsync(Validator validator)
        {
            if (Validations != null)
            {
                foreach (var c in Validations)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            if (NegativeChildren != null)
            {
                foreach (var c in NegativeChildren)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            if (ConjunctiveChildren != null)
            {
                foreach (var c in ConjunctiveChildren)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            if (DisjunctiveChildren != null)
            {
                foreach (var c in DisjunctiveChildren)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            return false;
        }
    }



    public delegate Task<bool> Validator(Validation validation);
    public interface IValidatable
    {
        Task<bool> ValidateAsync(Validator validator);
    }
}
