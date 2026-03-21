using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentAssessment.Core.Enums;

namespace StudentAssessment.Core.Entities
{
    public class Exam
    {
        public Guid Id {get; set;}
        public string Name {get;set;} = string.Empty;
        public ExamType Type {get;set;}
        public Guid ClassId {get; set;}


        // Navigation
        public Class Class {get; set; } = null!;
        public ICollection<Mark> Marks = [];
        public ICollection<Ranking> Rankings = [];
        
    }
}