using System;
using System.Collections.Generic;
using System.Text;

namespace RadarSoft.RadarCube.Html
{
    public class HtmlTextWriter
    {
        private readonly Stack<string> _tagsInRendering = new Stack<string>();

        private readonly Dictionary<string, string> fAttributes = new Dictionary<string, string>();
        private readonly StringBuilder fResponce = new StringBuilder();
        private readonly Dictionary<string, string> fStyleAttributes = new Dictionary<string, string>();

        private string _currentTag => _tagsInRendering.Peek();

        public string Responce => fResponce.ToString();


        public void Write(string line)
        {
            fResponce.AppendLine(line);
        }

        public void WriteInLine(string line)
        {
            fResponce.Append(line);
        }


        public void WriteBreak()
        {
            fResponce.AppendLine("<br>");
        }

        private string WriteAllAttributes()
        {
            var allAttrs = new StringBuilder();
            var attrsStrings = new List<string>();
            var styleAttrsStrings = new List<string>();

            foreach (var attrs in fAttributes)
                if (!string.IsNullOrEmpty(attrs.Value))
                    attrsStrings.Add(string.Format("{0}=\"{1}\"", attrs.Key, attrs.Value));
                else
                    attrsStrings.Add(attrs.Key);

            allAttrs.Append(string.Join(" ", attrsStrings.ToArray()));

            if (fStyleAttributes.Count > 0)
            {
                foreach (var styleAttrs in fStyleAttributes)
                    styleAttrsStrings.Add(string.Format("{0}: {1};", styleAttrs.Key, styleAttrs.Value));

                allAttrs.AppendFormat(" style='{0}'", string.Join(" ", styleAttrsStrings.ToArray()));
            }

            return allAttrs.ToString();
        }

        public void RenderBeginTag(string tag)
        {
            _tagsInRendering.Push(tag.ToLower());

            if (fAttributes.Count > 0 || fStyleAttributes.Count > 0)
            {
                fResponce.AppendFormat("<{0} {1}>", tag.ToLower(), WriteAllAttributes());
                fAttributes.Clear();
                fStyleAttributes.Clear();
            }
            else
            {
                fResponce.AppendFormat("<{0}>", tag.ToLower());
            }
        }

        public void RenderEndTag(string tag = "")
        {
            if (string.IsNullOrEmpty(tag))
            {
                if (_tagsInRendering.Count == 0)
                    throw new Exception(string.Format(
                        "An attempt to add a closing tag '{0}' without adding an opening tag!", tag.ToLower()));

                if (IsEndTagNeeded(_currentTag))
                    fResponce.AppendFormat("</{0}>", _currentTag);

                _tagsInRendering.Pop();
                return;
            }

            if (_currentTag != tag.ToLower())
                throw new Exception(string.Format("The closing tag '{0}' does not match the opening tag '{1}'!",
                    tag.ToLower(), _currentTag));

            if (IsEndTagNeeded(_currentTag))
                fResponce.AppendFormat("</{0}>", _currentTag);

            _tagsInRendering.Pop();
        }

        private bool IsEndTagNeeded(string tag)
        {
            return !(
                tag == "input" ||
                tag == "img"
            );
        }

        public void AddAttribute(string name, string value = "")
        {
            fAttributes[name] = value;
        }

        public void AddStyleAttribute(string name, string value)
        {
            fStyleAttributes[name] = value;
        }

        public override string ToString()
        {
            return Responce;
        }
    }
}