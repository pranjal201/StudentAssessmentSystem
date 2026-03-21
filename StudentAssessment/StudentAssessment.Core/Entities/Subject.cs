using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentAssessment.Core.Entities
{
    public class Subject
    {
        public string Code {get;set;} = string.Empty;
        public string Name {get; set;} = string.Empty;

        public ICollection<Mark> Marks {get;set;} =[];
    }
}