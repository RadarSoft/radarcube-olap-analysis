using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using RadarSoft.RadarCube.Controls.Analysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls.TagHelpers
{
    public class MOlapAnalysisTagHelper : MOlapAnalysis
    {
        public MOlapAnalysisTagHelper(HttpContext httpContext, IHostingEnvironment hosting, IMemoryCache cache) :
    base(httpContext, hosting, cache)
        {
            InitOlap += OlapAnalysis_InitOlap;
        }


        public MOlapAnalysisTagHelper(IHttpContextAccessor httpContextAccessor, IHostingEnvironment hosting, IMemoryCache cache) : 
            base(httpContextAccessor.HttpContext, hosting, cache)
        {
            InitOlap += OlapAnalysis_InitOlap;
        }

        public MOlapAnalysisTagHelper(IHttpContextAccessor httpContextAccessor, IHostingEnvironment hosting) :
            this(httpContextAccessor, hosting, null)
        {
        }

        public string TagName { get; set; } = "div";

        private void OlapAnalysis_InitOlap(object sender, EventArgs e)
        {
            Init();
        }

        public virtual void Init()
        {
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = TagName;
            output.Content.SetHtmlContent(Render().Value);
        }

        public virtual MOlapAnalysisTagHelper Clone()
        {
            var clone = new MOlapAnalysisTagHelper(null as HttpContext, null, null);
            CopyProperties(this, clone);
            return clone;
        }

        private void CopyProperties(object sourceObj, object targetObj)
        {
            var pFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var sourceProps = sourceObj.GetType().GetProperties(pFlags);

            foreach (var sProp in sourceProps)
            {
                if (sProp.GetSetMethod() == null || !sProp.PropertyType.GetTypeInfo().IsPrimitive)
                    continue;

                var tProp = targetObj.GetType().GetProperties(pFlags).FirstOrDefault(x => x.Name == sProp.Name);

                if (tProp == null)
                    continue;

                if (sProp.PropertyType.GetTypeInfo().IsEnum)
                {
                    tProp.SetValue(targetObj, (int) sProp.GetValue(sourceObj, null));
                    continue;
                }

                tProp.SetValue(targetObj, sProp.GetValue(sourceObj, null));
            }
        }
    }
}