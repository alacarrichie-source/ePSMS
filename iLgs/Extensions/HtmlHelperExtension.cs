using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace iLgs.Extensions
{
    public static class HtmlHelperExtension
    {
        class NavLinkMenuView : IDisposable
        {
            private HtmlHelper helper;
            private TagBuilder tagLi;
            private TagBuilder tagUl;

            public NavLinkMenuView(HtmlHelper helper)
            {
                this.helper = helper;

                tagLi = new TagBuilder("li");
                tagUl = new TagBuilder("ul");
            }

            public void Dispose()
            {
                this.helper.ViewContext.Writer.Write(tagUl.ToString(TagRenderMode.EndTag) + Environment.NewLine);
                this.helper.ViewContext.Writer.Write(tagLi.ToString(TagRenderMode.EndTag) + Environment.NewLine);
            }
        }

        public static MvcHtmlString NavLink(this HtmlHelper helper, string linkText, string actionName, string controllerName)
        {
            TagBuilder tagLi;

            tagLi = new TagBuilder("li");

            if ((helper.ViewData.ContainsKey("PageName") == true) && (helper.ViewData["PageName"].ToString().Equals(linkText) == true))
            {
                tagLi.AddCssClass("active");
            }

            return MvcHtmlString.Create(tagLi.ToString(TagRenderMode.StartTag) + LinkExtensions.ActionLink(helper, linkText, actionName, controllerName).ToString() + tagLi.ToString(TagRenderMode.EndTag));
        }

        public static MvcHtmlString NavLinkDivider()
        {
            TagBuilder tagLi;

            tagLi = new TagBuilder("li");
            tagLi.AddCssClass("x-divider-vertical");

            return MvcHtmlString.Create(tagLi.ToString(TagRenderMode.StartTag) + tagLi.ToString(TagRenderMode.EndTag));
        }

        public static IDisposable NavLinkMenu(this HtmlHelper helper, string linkText)
        {
            TagBuilder tagLi;
            TagBuilder tagA;
            TagBuilder tagSpan;
            TagBuilder tagUl;

            tagLi = new TagBuilder("li");
            tagA = new TagBuilder("a");
            tagSpan = new TagBuilder("span");
            tagUl = new TagBuilder("ul");

            tagLi.AddCssClass("dropdown");

            if ((helper.ViewData.ContainsKey("PageGroup") == true) && (helper.ViewData["PageGroup"].ToString().Equals(linkText) == true))
            {
                tagLi.AddCssClass("active");
            }

            tagA.AddCssClass("dropdown-toggle");

            tagA.MergeAttribute("data-toggle", "dropdown");
            tagA.MergeAttribute("href", "#");

            tagSpan.AddCssClass("caret");

            tagUl.AddCssClass("dropdown-menu");

            helper.ViewContext.Writer.Write(tagLi.ToString(TagRenderMode.StartTag) + Environment.NewLine);
            helper.ViewContext.Writer.Write(tagA.ToString(TagRenderMode.StartTag) + Environment.NewLine);
            helper.ViewContext.Writer.Write(linkText + Environment.NewLine);
            helper.ViewContext.Writer.Write(tagSpan.ToString(TagRenderMode.StartTag));
            helper.ViewContext.Writer.Write(tagSpan.ToString(TagRenderMode.EndTag) + Environment.NewLine);
            helper.ViewContext.Writer.Write(tagA.ToString(TagRenderMode.EndTag) + Environment.NewLine);
            helper.ViewContext.Writer.Write(tagUl.ToString(TagRenderMode.StartTag) + Environment.NewLine);

            return new NavLinkMenuView(helper);
        }

        public static MvcHtmlString NavLinkMenuDivider()
        {
            TagBuilder tagLi;

            tagLi = new TagBuilder("li");
            tagLi.AddCssClass("divider");

            return MvcHtmlString.Create(tagLi.ToString(TagRenderMode.StartTag) + tagLi.ToString(TagRenderMode.EndTag));
        }
    }
}