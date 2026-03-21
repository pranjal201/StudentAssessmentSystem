using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class Section
    {
        public Guid Id {get; set;}
        public Guid ClassId {get;set;}
        public string Name {get; set;} = string.Empty;

        // Navigation
        public Class Class {get;set;} = null!;
        public ICollection<Student> Students {get;set;} = [];
        public ICollection<TeacherSection> TeacherSections {get; set;} = [];
    }
}