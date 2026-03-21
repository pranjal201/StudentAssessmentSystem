using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class Student
    {
        public Guid Id {get;set;}
        public string FirstName {get;set;} = string.Empty;
        public string LastName {get;set;} = string.Empty;
        public Guid ClassId {get;set;}
        public Guid SectionId {get;set;}
        public Guid? UserId {get;set;}
        public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
        

        //Navigation
        public Class Class {get;set;} = null!;
        public Section Section {get;set;} = null!;
        public User? User {get;set;} = null!;
        public ICollection<Mark> Marks = [];
        public ICollection<Ranking> Rankings = [];
    }
}