using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InformationSystem.Models
{
    public class PumpListItem
    {
        public string Name { get; set; }
        public object  Value { get; set; }
    }
    public class CreateInfoObject {
        public int idClass { get; set; }
        public int idParent { get; set; }
        public int ChildId { get; set; }
        public string Name { get; set; }

        public List<Attribute> Params { get; set; }
    }
    public class Attribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}