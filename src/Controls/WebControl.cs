using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using RadarSoft.RadarCube.Html;
using System.IO;

namespace RadarSoft.RadarCube.Controls
{
    public abstract class WebControl : TagHelper
    {
        protected WebControl(HttpContext context, IHostingEnvironment hosting, IMemoryCache cache)
            : this(context, hosting)
        {
            Cache = cache;
        }

        protected WebControl(HttpContext context, IHostingEnvironment hosting)
        {
            Context = context;
            Hosting = hosting;
        }


        internal HttpContext Context {
            get;
            set;
        }

        internal IMemoryCache Cache { get; set; }

        internal IHostingEnvironment Hosting { get; set; }

        internal ISession Session => Context.Session;

        internal HttpRequest Request => Context.Request;

        internal HttpResponse Response => Context.Response;


        public virtual string ID { get; set; } = "";

        public string UniqueID => ID;

        public string ClientID => ID;

        public virtual string Width { get; set; } = "100%";

        public virtual string Height { get; set; } = "500px";

        internal string MapPath(string virtualPath)
        {
            if (Hosting == null)
                return virtualPath;

            return Path.Combine(Hosting.WebRootPath, virtualPath);
        }

        internal virtual void RenderControl(HtmlTextWriter writer)
        {
            Render(writer);
        }

        protected virtual void Render(HtmlTextWriter writer)
        {
            RenderBeginTag(writer);
            RenderContents(writer);
            RenderEndTag(writer);
        }

        public virtual void RenderBeginTag(HtmlTextWriter writer)
        {
        }

        protected virtual void RenderContents(HtmlTextWriter writer)
        {
        }

        public virtual void RenderEndTag(HtmlTextWriter writer)
        {
        }

        protected virtual void CreateChildControls()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}