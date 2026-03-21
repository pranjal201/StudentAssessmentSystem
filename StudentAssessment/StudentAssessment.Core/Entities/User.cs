using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentAssessment.Core.Enums;

namespace StudentAssessment.Core.Entities
{
    public class User
    {
        public Guid Id {get; set;}
        public string Username {get;set;} = string.Empty;
        public string PasswordHash {get;set;} = string.Empty;
        public UserRole Role {get;set;}
        public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

        //Navigation
        public ICollection<TeacherSection> TeacherSections {get; set;} = [];


    }
}