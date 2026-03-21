using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class Mark
    {
       public Guid Id {get; set;} 
       public Guid StudentId {get;set;}
       public Guid SubjectId {get;set;}
       public Guid ExamId {get;set;}
       public decimal Score {get;set;}
       public string IdempotencyKey {get; set;} = string.Empty;
       public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
       public DateTime UpdatedAt {get;set;} = DateTime.UtcNow;

       //Navigation

       public Student Student {get;set;} = null!;
       public Subject Subject {get;set;} = null!;
       public Exam Exam {get; set;} = null!;
    }
}