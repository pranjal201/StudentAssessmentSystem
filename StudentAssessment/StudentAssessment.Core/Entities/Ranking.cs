using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class Ranking
    {
        public Guid StudentId {get;set;}
        public Guid ExamId {get;set;}
        public decimal TotalMarks {get;set;}
        public int SectionRank {get;set;}
        public int ClassRank {get;set;}
        public DateTime UpdatedAt {get;set ;} = DateTime.UtcNow;


        //Navigation
        public Student Student {get; set;}= null!;
        public Exam Exam {get; set;}= null!;
        
    }
}