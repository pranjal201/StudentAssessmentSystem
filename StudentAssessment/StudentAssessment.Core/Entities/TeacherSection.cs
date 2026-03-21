using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class TeacherSection
    {
        public Guid TeacherId {get;set;}
        public Guid SectionId {get;set;}

        //Navigation
        public User Teacher {get;set;} = null!;
        public Section Section {get;set;} = null!;
    }
}