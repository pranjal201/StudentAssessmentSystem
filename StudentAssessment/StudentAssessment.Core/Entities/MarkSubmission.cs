using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentAssessment.Core.Enums;

namespace StudentAssessment.Core.Entities
{
    public class MarkSubmission
    {
        public Guid Id {get;set;}
        public string Payload {get; set;} = string.Empty;
        public JobStatus Status {get;set;}
        public int RetryCount {get; set;} = 0;
        public DateTime? NextRetryAt {get;set;}
        public string CorrelationId {get;set;} = string.Empty;
        public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
        public DateTime? ProcessedAt {get; set;}
    }
}