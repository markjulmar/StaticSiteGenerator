using System.Collections.Generic;
using System.Text;

namespace XamU.SGL.Extensions.MarkdownDeep
{
    public class SpecialAttributes : Dictionary<string,string>
    {
        public string Id
        {
            get
            {
                return this["id"];
            }
            set
            {
                this["id"] = value;
            }
        }

        public string ClasssNames
        {
            get
            {
                return this["class"];
            }
        }

        public void AddClass(string className)
        {
            if (ContainsKey("class"))
            {
                this["class"] = this["class"] + " " + className.Trim();
            }
            else
            {
                this["class"] = className.Trim();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var key in this.Keys)
            {
                sb.AppendFormat(" {0}=\"{1}\"", key, this[key]);
            }
            return sb.ToString();
        }
    }
}

