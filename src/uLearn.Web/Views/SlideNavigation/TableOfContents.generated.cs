﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace uLearn.Web.Views.SlideNavigation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using uLearn;
    using uLearn.Web.Models;
    using uLearn.Web.Views.Course;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public static class TableOfContents
    {

public static System.Web.WebPages.HelperResult Toc(TocModel toc)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t<ul class=\"nav\">\r\n\t\t<li class=\"units-list-item full-width units-list-item-text c" +
"ourse-name\"><a data-score=\"");


                                              WebViewPage.WriteTo(@__razor_helper_writer, SlideHtml.Score(toc.Score, toc.MaxScore));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\">");


                                                                                         WebViewPage.WriteTo(@__razor_helper_writer, toc.Course.Title);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</a></li>\r\n");


  		
			int iUnit = 0;
			foreach (var unit in toc.Units)
			{
				
WebViewPage.WriteTo(@__razor_helper_writer, TocUnit(unit, iUnit));

                         
				iUnit++;
			}
		

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t</ul>\r\n");



});

}


public static System.Web.WebPages.HelperResult TocUnit(TocUnitModel unit, int index)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 
	var collapseOption = unit.IsCurrent ? "collapse in" : "collapse";

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t<li class=\"units-list-item clickable full-width\">\r\n\t\t<a data-toggle=\"collapse\" h" +
"ref=");


WebViewPage.WriteTo(@__razor_helper_writer, "#N" + index);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, " class=\"units-list-item-text no-smooth-scrolling\" data-score=\"");


                                                                   WebViewPage.WriteTo(@__razor_helper_writer, SlideHtml.Score(unit.Score, unit.MaxScore));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\">");


                                                                                                                WebViewPage.WriteTo(@__razor_helper_writer, unit.UnitName);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</a>\r\n\t\t<ul id=");


WebViewPage.WriteTo(@__razor_helper_writer, "N" + index);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, " class=\"slides-list ");


WebViewPage.WriteTo(@__razor_helper_writer, collapseOption);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\">\r\n");


 			foreach (var page in unit.Pages)
			{
				
WebViewPage.WriteTo(@__razor_helper_writer, TocItem(page));

                  
			}


 			foreach (var kv in unit.AdditionalScores)
			{
				var scoringGroup = kv.Key;
				var score = kv.Value;
				
WebViewPage.WriteTo(@__razor_helper_writer, TocAdditionalScore(scoringGroup, score));

                                            
			}

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t\t</ul>\r\n\t</li>\r\n");



});

}


public static System.Web.WebPages.HelperResult TocItem(TocPageInfo page)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t<li class=\"slide-list-item clickable ");


WebViewPage.WriteTo(@__razor_helper_writer, page.IsCurrent ? "selected" : "");

WebViewPage.WriteLiteralTo(@__razor_helper_writer, " full-size\" onclick=\"window.location.href=\'");


                                                                         WebViewPage.WriteTo(@__razor_helper_writer, page.Url);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\'\" data-slide-id=\"");


                                                                                                    WebViewPage.WriteTo(@__razor_helper_writer, page.SlideId);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\">\r\n\t\t<i class=\"");


WebViewPage.WriteTo(@__razor_helper_writer, GetPageIconClass(page));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, " navbar-label\" title=\"");


              WebViewPage.WriteTo(@__razor_helper_writer, GetTocPageTytle(page.PageType));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\"></i>\r\n\t\t<a href=\"");


WebViewPage.WriteTo(@__razor_helper_writer, page.Url);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\" style=\"margin-right: 5px\">");


     WebViewPage.WriteTo(@__razor_helper_writer, page.Name);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</a>\r\n\t\t<span class=\"score right\">");


WebViewPage.WriteTo(@__razor_helper_writer, SlideHtml.Score(page.Score, page.MaxScore));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</span>\r\n\t</li>\r\n");



});

}


public static System.Web.WebPages.HelperResult TocAdditionalScore(ScoringGroup group, int score)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t<li class=\"slide-list-item full-size\">\r\n\t\t<i class=\"navbar-label\" title=\"\"></i>\r" +
"\n\t\t<span style=\"margin-right: 5px\">");


WebViewPage.WriteTo(@__razor_helper_writer, group.Name);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</span>\r\n\t\t<span class=\"score right\">");


WebViewPage.WriteTo(@__razor_helper_writer, SlideHtml.Score(score, group.MaxAdditionalScore));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</span>\r\n\t</li>\r\n");



});

}



	private static string GetTocPageTytle(TocPageType pageType)
	{
		if (pageType == TocPageType.Theory) return "Теория";
		if (pageType == TocPageType.Exercise) return "Задача";
		if (pageType == TocPageType.Quiz) return "Тест";
		return "";
	}


public static System.Web.WebPages.HelperResult GetPageIconClass(TocPageInfo page)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 
	string progressClass;
	string typeClass;
	if (page.PageType == TocPageType.InstructorNotes || page.PageType == TocPageType.Statistics)
	{
		typeClass = "glyphicon-eye-close";
		progressClass = "navbar-label-privileged";
	}
	else
	{
		var isComplete = !page.ShouldBeSolved || page.IsSolved;
		progressClass = isComplete ? "navbar-label-success"
			: (page.IsVisited ? "navbar-label-danger" : "navbar-label-default");
		typeClass = page.PageType == TocPageType.Quiz ? "glyphicon-pushpin"
			: page.PageType == TocPageType.Exercise ? (isComplete ? "glyphicon-check" : "glyphicon-edit")
				: (page.IsVisited ? "glyphicon-ok" : "glyphicon-none");
	}
	
WebViewPage.WriteTo(@__razor_helper_writer, progressClass + " glyphicon " + typeClass);

                                             

});

}


    }
}
#pragma warning restore 1591
