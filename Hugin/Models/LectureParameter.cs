using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Xml.Serialization;

namespace Hugin.Models
{

    [Serializable, XmlRoot("Parameters")]
    public class LectureParameters
    {
        [XmlElement("DateTime", typeof(LectureDateTimeParameter))]
        [XmlElement("String", typeof(LectureStringParameter))]
        [XmlElement("Boolean", typeof(LectureBooleanParameter))]
        [XmlElement("Integer", typeof(LectureIntegerParameter))]
        [XmlElement("Double", typeof(LectureDoubleParameter))]
        public object[] Parameters { get; set; } = new object[0];


        public IEnumerable<ILectureParameter> GetValues()
        {
            return Parameters != null ? Parameters.Cast<ILectureParameter>() : new ILectureParameter[] { };
        }
        public T GetValue<T>(string name) where T : ILectureParameter
        {
            return Parameters.Where(p => p is T).Cast<T>().FirstOrDefault(p => p?.Name == name);
        }
        public void SetValue<T>(string name, T value) where T : ILectureParameter
        {
            var p = Parameters.Where(p => p is T).Cast<T>().FirstOrDefault(p => p?.Name == name);

        }
    }

    public interface ILectureParameter
    {
        string Name { get; }
        string Description { get; }
        ParameterTypes DataType { get; }
        dynamic GetValue();
    }
    [Serializable]
    public class LectureDateTimeParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }

        [XmlIgnore]
        public DateTime? Value { get; set; }

        [XmlText]
        public string _Value
        {
            get
            {

                return Value?.ToString("yyyy/MM/dd HH:mm:ss");
            }
            set
            {
                if (DateTime.TryParse(value, out var dt))
                {
                    Value = dt;
                }
                else
                {
                    Value = null;
                }
            }
        }





        public dynamic GetValue() { return Value; }
        public ParameterTypes DataType { get { return ParameterTypes.DateTime; } }
    }
    [Serializable]
    public class LectureStringParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public string Value { get; set; }

        public dynamic GetValue() { return Value; }
        public ParameterTypes DataType { get { return ParameterTypes.String; } }
    }
    [Serializable]
    public class LectureBooleanParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public bool Value { get; set; }

        public dynamic GetValue() { return Value; }
        public ParameterTypes DataType { get { return ParameterTypes.Boolean; } }
    }
    [Serializable]
    public class LectureIntegerParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public long Value { get; set; }

        public dynamic GetValue() { return Value; }
        public ParameterTypes DataType { get { return ParameterTypes.Integer; } }
    }
    [Serializable]
    public class LectureDoubleParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public double Value { get; set; }

        public dynamic GetValue() { return Value; }
        public ParameterTypes DataType { get { return ParameterTypes.Double; } }
    }




}
