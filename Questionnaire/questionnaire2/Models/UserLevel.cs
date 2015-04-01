using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Questionnaire2.Models
{
    public class UserLevel
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FinalStepLevel { get; set; }
        public DateTime FinalStepLevelDate { get; set; }
    }
}