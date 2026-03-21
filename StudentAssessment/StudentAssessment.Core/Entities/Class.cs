using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class Class
    {
        public Guid Id {get;set;}
        public string Name {get;set;} = string.Empty;


        //Navigation
        public ICollection<Section> Sections {get; set;} = [];
        public ICollection<Student> Students {get;set;} = [];
        public ICollection<Exam> Exams {get; set;} = [];
    
    }
}