using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InformationSystem.Models
{
    public class ObjectHierarchy
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Id { get; set; }
        public int? ParentId { get; set; }
    }
}