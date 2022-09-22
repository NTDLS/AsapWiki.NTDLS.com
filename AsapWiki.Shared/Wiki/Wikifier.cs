﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Classes;
using System.Web;
using AsapWiki.Shared.Repository;

namespace AsapWiki.Shared.Wiki
{
    public class Wikifier
    {
        class MatchSet
        {
            public string Content { get; set; }
            /// <summary>
            /// The content in this segment will not be wikified.
            /// </summary>
            public bool AllowNestedDecode { get; set; }
        }

        private int _matchesPerIteration = 0;
        private List<string> _tags = new List<string>();
        private Dictionary<string, MatchSet> _lookup;


        private readonly string _tocName = "TOC_" + (new Random()).Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new List<TOCTag>();
        private Page _page;
        private readonly StateContext _context;

        public Wikifier(StateContext context)
        {
            _context = context;
        }

        public string Transform(Page page)
        {
            _page = page;

            _lookup = new Dictionary<string, MatchSet>();

            var pageContent = new StringBuilder(page.Body);

            TransformLiterals(pageContent);

            while (TransformAll(pageContent) > 0)
            {
            }

            TransformPostProcess(pageContent);
            TransformWhitespace(pageContent);

            int length;
            do
            {
                length = pageContent.Length;
                foreach (var v in _lookup)
                {
                    pageContent.Replace(v.Key, v.Value.Content);
                }
            } while (length != pageContent.Length);

            return pageContent.ToString();
        }

        public int TransformAll(StringBuilder pageContent)
        {
            _matchesPerIteration = 0;

            TransformSections(pageContent);
            TransformInnerLinks(pageContent);
            TransformMarkup(pageContent);
            TransformSectionHeadings(pageContent);
            TransformFunctions(pageContent);
            TransformProcessingInstructions(pageContent);

            //We have to replace a few times because we could have replace tags (guids) nested inside others.
            int length;
            do
            {
                length = pageContent.Length;
                foreach (var v in _lookup)
                {
                    if (v.Value.AllowNestedDecode)
                    {
                        pageContent.Replace(v.Key, v.Value.Content);
                    }
                }
            } while (length != pageContent.Length);

            return _matchesPerIteration;
        }

        private void StoreError(StringBuilder pageContent, string match, string value)
        {
            _matchesPerIteration++;

            string identifier = "{" + Guid.NewGuid().ToString() + "}";

            var matchSet = new MatchSet()
            {
                Content = $"<i><font size=\"3\" color=\"#BB0000\">{{{value}}}</font></a>",
                AllowNestedDecode = false
            };

            _lookup.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }

        private void StoreMatch(StringBuilder pageContent, string match, string value, bool allowNestedDecode = true)
        {
            _matchesPerIteration++;

            string identifier = "{" + Guid.NewGuid().ToString() + "}";

            var matchSet = new MatchSet()
            {
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            _lookup.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }

        private void StoreMatch(StringBuilder pageContent, int startPosition, int length, string value, bool allowNestedDecode = true)
        {
            _matchesPerIteration++;

            string identifier = "{" + Guid.NewGuid().ToString() + "}";

            var matchSet = new MatchSet()
            {
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            _lookup.Add(identifier, matchSet);
            pageContent.Remove(startPosition, length);
            pageContent.Insert(startPosition, identifier);
        }

        /*
        private void TransformHashtags()
        {
            //Remove hashtags, they are stored with the page but not displayed.
            Regex rgx = new Regex(@"(?:\s|^)#[A-Za-z0-9\-_\.]+", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                StoreMatch(pageContent, match.Value, String.Empty);
            }
        }
        */

        private void TransformWhitespace(StringBuilder pageContent)
        {
            pageContent.Replace("\r\n", "\n");

            /*
            int length;
            do
            {
                length = pageContent.Length;
                pageContent.Replace("\n\n", "\n");
            } while (pageContent.Length != length);
            */

            pageContent.Replace("\n", "<br />");
        }

        /// <summary>
        /// Replaces HTML where we are transforming the entire line, such as "*this will be bold" - > "<b>this will be bold</b>
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="htmlTag"></param>
        void ReplaceWholeLineHTMLMarker(StringBuilder pageContent, string mark, string htmlTag, bool escape)
        {
            string marker = String.Empty;
            if (escape)
            {
                foreach (var c in mark)
                {
                    marker += $"\\{c}";
                }
            }
            else
            {
                marker = mark;
            }

            Regex rgx = new Regex($"^{marker}.*?\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            //We roll-through these matches in reverse order because we are replacing by position. We don't move the earlier positions by replacing from the bottom up.
            for (int i = matches.Count - 1; i > -1; i--)
            {
                var match = matches[i];
                string value = match.Value.Substring(mark.Length, match.Value.Length - mark.Length).Trim();
                var matxhString = match.Value.Trim(); //We trim the match because we are matching to the end of the line which includes the \r\n, which we do not want to replace.
                StoreMatch(pageContent, match.Index, matxhString.Length, $"<{htmlTag}>{value}</{htmlTag}> ");
            }
        }

        void ReplaceInlineHTMLMarker(StringBuilder pageContent, string mark, string htmlTag, bool escape)
        {
            string marker = String.Empty;
            if (escape)
            {
                foreach (var c in mark)
                {
                    marker += $"\\{c}";
                }
            }
            else
            {
                marker = mark;
            }

            Regex rgx = new Regex($@"{marker}.*?{marker}", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - (mark.Length * 2));

                StoreMatch(pageContent, match.Value, $"<{htmlTag}>{value}</{htmlTag}>");
            }
        }

        private void TransformMarkup(StringBuilder pageContent)
        {
            ReplaceWholeLineHTMLMarker(pageContent, "**", "strong", true); //Single line bold.
            ReplaceWholeLineHTMLMarker(pageContent, "__", "u", false); //Single line underline.
            ReplaceWholeLineHTMLMarker(pageContent, "//", "i", true); //Single line italics.
            ReplaceWholeLineHTMLMarker(pageContent, "!!", "mark", true); //Single line highlight.

            ReplaceInlineHTMLMarker(pageContent, "**", "strong", true); //inline bold.
            ReplaceInlineHTMLMarker(pageContent, "__", "u", false); //inline highlight.
            ReplaceInlineHTMLMarker(pageContent, "//", "i", true); //inline highlight.
            ReplaceInlineHTMLMarker(pageContent, "!!", "mark", true); //inline highlight.
        }

        private void TransformLiterals(StringBuilder pageContent)
        {
            //Transform literal strings, even encodes HTML so that it displays verbatim.
            Regex rgx = new Regex(@"\[\{([\S\s]*?)\}\]", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(2, match.Value.Length - 4);
                value = HttpUtility.HtmlEncode(value);
                StoreMatch(pageContent, match.Value, value.Replace("\r", "").Replace("\n", "<br />"), false);
            }
        }

        private void TransformSections(StringBuilder pageContent)
        {
            //Transform panels.
            Regex rgx = new Regex(@"\{\{\{\(([\S\s]*?)\}\}\}", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6).Trim();

                int newlineIndex = value.IndexOf(')');

                if (newlineIndex > 0)
                {
                    string firstLine = value.Substring(0, newlineIndex + 1).Trim();
                    string content = value.Substring(newlineIndex + 1).Trim();
                    string boxType;
                    string title = String.Empty;
                    bool allowNestedDecode = true;
                    if (firstLine.StartsWith("(") && firstLine.EndsWith(")"))
                    {
                        firstLine = firstLine.Substring(1, firstLine.Length - 2);

                        //Parse box type and title.
                        int index = firstLine.IndexOf(",");
                        if (index > 0) //Do we have a title? Only applicable for some of the box types really...
                        {
                            title = firstLine.Substring(index + 1).Trim();
                            boxType = firstLine.Substring(0, index).Trim();
                        }
                        else
                        {
                            boxType = firstLine.Trim();
                        }

                        var html = new StringBuilder();

                        switch (boxType.ToLower())
                        {
                            case "alert":
                            case "alert-default":
                            case "alert-info":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-info\">{content}.</div>");
                                }
                                break;
                            case "alert-danger":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-danger\">{content}.</div>");
                                }
                                break;
                            case "alert-warning":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-warning\">{content}.</div>");
                                }
                                break;
                            case "alert-success":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-success\">{content}.</div>");
                                }
                                break;

                            case "jumbotron":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"jumbotron\">{content}.</div>");
                                }
                                break;

                            case "panel":
                            case "panel-default":
                                {
                                    html.Append("<div class=\"panel panel-default\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-primary":
                                {
                                    html.Append("<div class=\"panel panel-primary\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-success":
                                {
                                    html.Append("<div class=\"panel panel-success\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-info":
                                {
                                    html.Append("<div class=\"panel panel-info\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-warning":
                                {
                                    html.Append("<div class=\"panel panel-warning\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-danger":
                                {
                                    html.Append("<div class=\"panel panel-danger\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                        }
                        StoreMatch(pageContent, match.Value, html.ToString(), allowNestedDecode);
                    }
                }
            }

            /*
            TransformSyntaxHighlighters("cpp", "cpp");
            TransformSyntaxHighlighters("csharp", "C#");
            TransformSyntaxHighlighters("sql", "sql");
            TransformSyntaxHighlighters("vbnet", "vbnet");
            TransformSyntaxHighlighters("xml", "xml");
            TransformSyntaxHighlighters("css", "css");
            TransformSyntaxHighlighters("java", "java");
            */
        }

        /*
        private void TransformSyntaxHighlighters(string tag, string brush)
        {
            Regex rgx = new Regex("\\[\\[" + tag + "\\]\\]([\\s\\S]*?)\\[\\[\\/" + tag + "\\]\\]", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string rawValue = match.Value.Substring(tag.Length + 4, match.Value.Length - ((tag.Length * 2) + 9));
                rawValue = rawValue.Replace("<", "&lt;").Replace(">", "&gt;");
                StoreMatch(pageContent, match.Value, "<pre class='brush: " + brush + "; toolbar: false; auto-links: false;'>" + rawValue + "</pre>");
            }
        }
        */

        void TransformSectionHeadings(StringBuilder pageContent)
        {
            var regEx = new StringBuilder();
            regEx.Append(@"(\=\=\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=.*?\n)");

            Regex rgx = new Regex(regEx.ToString(), RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                int equalSigns = 0;
                foreach (char c in match.Value)
                {
                    if (c != '=')
                    {
                        break;
                    }
                    equalSigns++;
                }
                if (equalSigns >= 2 && equalSigns <= 6)
                {
                    string tag = _tocName + "_" + _tocTags.Count().ToString();
                    string value = match.Value.Substring(equalSigns, match.Value.Length - equalSigns).Trim();

                    int fontSize = 8 - equalSigns;
                    if (fontSize < 5) fontSize = 5;

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (equalSigns - 1).ToString() + "\">" + value + "</span></a></font>";
                    StoreMatch(pageContent, match.Value.Trim(), link);
                    _tocTags.Add(new TOCTag(equalSigns - 1, match.Index, tag, value));
                }
            }
        }

        private void TransformInnerLinks(StringBuilder pageContent)
        {
            //Parse external explicit links. eg. [[http://test.net]].
            Regex rgx = new Regex(@"(\[\[http\:\/\/.+?\]\])", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    string linkText = keyword.Substring(pipeIndex + 1);

                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string border = "";

                        if (linkText.IndexOf("border", StringComparison.CurrentCultureIgnoreCase) < 0)
                        {
                            border = " border=\"0\"";
                        }

                        linkText = "<img " + linkText + border + ">";
                    }

                    keyword = keyword.Substring(0, pipeIndex);


                    StoreMatch(pageContent, match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>");
                }
                else
                {
                    StoreMatch(pageContent, match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>");
                }
            }

            //Parse internal dynamic links. eg [[AboutUs|About Us]].
            rgx = new Regex(@"(\[\[.+?\]\])", RegexOptions.IgnoreCase);
            matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                string explicitLinkText = "";
                string linkText;

                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    explicitLinkText = keyword.Substring(pipeIndex + 1);
                    keyword = keyword.Substring(0, pipeIndex);
                }

                string pageName = keyword;
                string pageNavigation = HTML.CleanFullURI(pageName).Replace("/", "");
                var page = Repository.PageRepository.GetPageByNavigation(pageNavigation);

                if (page != null)
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = page.Name;
                    }
                    else
                    {
                        linkText = explicitLinkText;
                    }

                    StoreMatch(pageContent, match.Value, "<a href=\"" + HTML.CleanFullURI($"/Wiki/Show/{pageNavigation}") + $"\">{linkText}</a>");
                }
                else if (_context.CanCreatePage())
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = pageName;
                    }
                    else
                    {
                        linkText = explicitLinkText;
                    }

                    linkText += "<font color=\"#cc0000\" size=\"2\">?</font>";
                    StoreMatch(pageContent, match.Value, "<a href=\"" + HTML.CleanFullURI($"/Wiki/Edit/{pageNavigation}/") + $"?Name={pageName}\">{linkText}</a>");
                }
                else
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = pageName;
                    }
                    else
                    {
                        linkText = explicitLinkText;
                    }

                    //Remove wiki tags for pages which were not found or which we do not have permission to view.
                    if (linkText.Length > 0)
                    {
                        StoreMatch(pageContent, match.Value, linkText);
                    }
                    else
                    {
                        StoreError(pageContent, match.Value, $"The page has no name for {keyword}");
                    }
                }
            }
        }

        private void TransformProcessingInstructions(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"(\@\@\w+)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 2).Trim();

                switch (keyword.ToLower())
                {
                    case "depreciate":
                        pageContent.Insert(0, "<div class=\"alert alert-danger\">This page has been depreciate and will be deleted.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "template":
                        pageContent.Insert(0, "<div class=\"alert alert-info\">This page is a template and will not appear in indexes or glossaries.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "include":
                        pageContent.Insert(0, "<div class=\"alert alert-info\">This page is an include and will not appear in indexes or glossaries.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "draft":
                        pageContent.Insert(0, "<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                }
            }
        }

        private string RemoveParens(string text)
        {
            return text.Substring(1, text.Length - 2);
        }

        private void TransformFunctions(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"\#\#[A-Za-z]*\(.*?\)|(\#\#.+?\(\))|(\#\#\w+)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());

            foreach (Match match in matches)
            {
                string keyword = string.Empty;
                List<string> args = new List<string>();

                MatchCollection rawargs = (new Regex(@"\(+?\)|\(.+?\)")).Matches(match.Value);
                if (rawargs.Count > 0)
                {
                    keyword = match.Value.Substring(2, match.Value.IndexOf('(') - 2).ToLower();

                    foreach (var rawarg in rawargs)
                    {
                        string rawArgTrimmed = rawarg.ToString().Substring(1, rawarg.ToString().Length - 2);
                        args.AddRange(rawArgTrimmed.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 2).ToLower(); ; //The match has no parameter.
                }

                switch (keyword)
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Includes a page by it's navigation link.
                    case "include": //(PageCategory\PageName)
                        {
                            if (args.Count != 1)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            Page page = GetPageFromPathInfo(args[0]);
                            if (page != null)
                            {
                                var wikify = new Wikifier(_context);

                                StoreMatch(pageContent, match.Value, wikify.Transform(page));
                            }
                            else
                            {
                                //Remove wiki tags for pages which were not found or which we do not have permission to view.
                                StoreMatch(pageContent, match.Value, "");
                            }
                        }
                        break;
                    //Associates tags with a page. These are saved with the page and can also be displayed.
                    case "settags": //##SetTags(comma,seperated,list,of,tags)
                        {
                            if (args.Count == 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            _tags.AddRange(args);
                            StoreMatch(pageContent, match.Value, "");
                        }
                        break;
                    //Displays an image that is attached to the page.
                    case "image": //##Image(Name, [optional:default=100]Scale, [optional:default=""]Alt-Text)
                        if (args != null && args.Count > 0)
                        {
                            if (args.Count < 1 || args.Count > 2)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            string imageName = args[0];
                            string scale = "100";
                            string alt = imageName;

                            if (args.Count > 1)
                            {
                                scale = args[1];
                            }
                            if (args.Count > 2)
                            {
                                alt = args[2];
                            }

                            string link = $"/Wiki/Png/{_page.Navigation}?Image={imageName}";
                            string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}&Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";

                            StoreMatch(pageContent, match.Value, image);
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of files attached to the page.
                    case "files": //##Files()
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            var files = PageFileRepository.GetPageFilesInfoByPageId(_page.Id);

                               var html = new StringBuilder();

                            if (files.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var file in files)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Download/{file.Name}\">{file.Name} ({file.FriendlySize})</a>");
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;


                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages that have been recently modified.
                    case "recentlymodified": //##RecentlyModified(TopCount)
                    case "recentlymodifiedfull": //##RecentlyModifiedFull(TopCount)
                        {
                            if (args.Count != 1)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            if (!int.TryParse(args[0], out int takeCount))
                            {
                                continue;
                            }

                            var pages = Repository.PageRepository.GetTopRecentlyModifiedPages(takeCount);

                            //If we specified a Top Count parameter, then we want to show the most recent pages
                            //  which were added to the category - otherwise we show ALL pages in the category so
                            //  we order them simply by name.
                            if (args.Count == 1)
                            {
                                pages = pages.OrderBy(p => p.Name).ToList();
                            }

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Show/{page.Navigation}\">{page.Name}</a>");

                                    if (keyword == "recentlymodifiedfull")
                                    {
                                        if (page.Description.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary of pages in the specified comma seperated category names.
                    case "categoryglossary": //(CategoryNames)
                    case "categoryglossaryfull": //(CategoryNames)
                        if (args != null && args.Count == 1)
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            string[] categoryName = args[0].ToLower().Split(',');

                            List<Page> pages = new List<Page>();

                            foreach (var searchString in categoryName)
                            {
                                var search = Repository.PageRepository.GetPagesByCategoryNavigation(searchString);
                                pages.AddRange(search);
                            }

                            var html = new StringBuilder();

                            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpper()).Distinct();

                            if (pages.Count() > 0)
                            {
                                html.Append("<center>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                                }
                                html.Append("</center>");

                                html.Append("<ul>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                    html.Append("<ul>");
                                    foreach (var page in pages.Where(p => p.Name.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        html.Append("<li><a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

                                        if (keyword == "categoryglossaryfull")
                                        {
                                            if (page.Description.Length > 0)
                                            {
                                                html.Append(" - " + page.Description);
                                            }
                                        }
                                        html.Append("</li>");
                                    }
                                    html.Append("</ul>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;
                    */
                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary by searching page's body text for the specified comma seperated list of words.
                    case "textglossary": //(PageSearchText)
                    case "textglossaryfull": //(PageSearchText)
                        if (args != null && args.Count == 1)
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            string[] searchStrings = args[0].ToLower().Split(',');

                            List<Page> pages = new List<Page>();

                            foreach (var searchString in searchStrings)
                            {
                                var search = Repository.PageRepository.GetPagesByBodyText(searchString);
                                pages.AddRange(search);
                            }

                            var html = new StringBuilder();

                            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpper()).Distinct();

                            if (pages.Count() > 0)
                            {
                                html.Append("<center>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                                }
                                html.Append("</center>");

                                html.Append("<ul>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                    html.Append("<ul>");
                                    foreach (var page in pages.Where(p => p.Name.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        html.Append("<li><a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

                                        if (keyword == "textglossaryfull")
                                        {
                                            if (page.Description.Length > 0)
                                            {
                                                html.Append(" - " + page.Description);
                                            }
                                        }
                                        html.Append("</li>");
                                    }
                                    html.Append("</ul>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;
                    */
                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page body for the specified text.
                    //  Optionally also only pulls n-number of pages ordered by decending by the last modified date (then by page name).
                    case "textlist": //(PageSearchText, [optional]TopCount)
                    case "textlistfull": //(PageSearchText, [optional]TopCount)
                        if (args != null && (args.Count == 1 || args.Count == 2))
                        {
                            string searchString = args[0].ToLower();
                            int takeCount = 100000;

                            if (args.Count > 1)
                            {
                                if (!int.TryParse(args[1], out takeCount))
                                {
                                    continue;
                                }
                            }

                            var pages = Repository.PageRepository.GetPagesByBodyText(searchString).Take(takeCount);

                            //If we specified a Top Count parameter, then we want to show the most recent
                            //  modified pages otherwise we show ALL pages simply ordered by name.
                            if (args.Count > 1)
                            {
                                pages = pages.OrderByDescending(p => p.ModifiedDate).ThenBy(p => p.Name);
                            }
                            else
                            {
                                pages = pages.OrderBy(p => p.Name);
                            }

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

                                    if (keyword == "textlistfull")
                                    {
                                        if (page.Description.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;
                    /*
                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of the most recently modified pages, optionally only returning the top n-pages.
                    case "recentlymodifiedfull": //(TopCount, [Optional]CategoryName)
                    case "recentlymodified": //(TopCount, [Optional]CategoryName)
                                             //Creates a list of the most recently created pages, optionally only returning the top n-pages.
                    case "recentlycreatedfull": //(TopCount, [Optional]CategoryName)
                    case "recentlycreated": //(TopCount, [Optional]CategoryName)
                        if (args != null && (args.Count == 1 || args.Count == 2))
                        {
                            string[] categoryNames = null;
                            int takeCount = 0;
                            if (!int.TryParse(args[0], out takeCount))
                            {
                                continue;
                            }

                            if (args.Count > 1)
                            {
                                categoryNames = args[1].ToLower().Split(',');
                            }

                            List<Page> pages = null;

                            if (categoryNames == null)
                            {
                                pages = Repository.PageRepository.GetAllPage();
                            }
                            else
                            {
                                pages = new List<Page>();

                                foreach (string categoryName in categoryNames)
                                {
                                    pages.AddRange(Repository.PageRepository.GetTopRecentlyModifiedPagesByCategoryNavigation(takeCount, categoryName));
                                }
                            }

                            if (keyword.ToLower().StartsWith("recentlymodified"))
                            {
                                pages = pages.OrderByDescending(p => p.ModifiedDate).ThenBy(p => p.Name).Take(takeCount).ToList();
                            }
                            else if (keyword.ToLower().StartsWith("recentlycreated"))
                            {
                                pages = pages.OrderByDescending(p => p.CreatedDate).ThenBy(p => p.Name).Take(takeCount).ToList();
                            }

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<table cellpadding=\"1\" cellspacing=\"0\" border=\"0\" width=\"100%\">");

                                foreach (var page in pages)
                                {
                                    string date = string.Empty;

                                    if (keyword.ToLower().StartsWith("recentlymodified"))
                                    {
                                        date = page.ModifiedDate.ToString("MM/dd/yyyy");
                                    }
                                    else if (keyword.ToLower().StartsWith("recentlycreated"))
                                    {
                                        date = page.CreatedDate.ToString("MM/dd/yyyy");
                                    }

                                    html.Append("<tr>");
                                    html.Append("<td class=\"WikiModTableHead\" valign=\"top\" width=\"100%\">");
                                    html.Append("<span class=\"WikiModSpanDate\">" + date.ToString() + "</span>");

                                    html.Append("&nbsp;<a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

                                    html.Append("</td>");
                                    html.Append("</tr>");

                                    html.Append("<tr>");

                                    if (keyword.ToLower().EndsWith("full"))
                                    {
                                        html.Append("<tr>");
                                        html.Append("<td class=\"WikiModTableDetail\" valign=\"top\">");
                                        if (page.Description.Length > 0)
                                        {
                                            html.Append(page.Description);
                                        }
                                        html.Append("</td>");
                                        html.Append("</tr>");

                                        html.Append("<tr>");
                                        html.Append("<td colspan=\"2\" valign=\"top\">");
                                        html.Append("<img src=\"/Images/Site/Spacer.gif\" height=\"1\" width=\"1\" />");
                                        html.Append("</td>");
                                        html.Append("</tr>");
                                    }
                                }

                                html.Append("</table>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;
                    */
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was last modified.
                    case "lastmodified":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            DateTime lastModified = DateTime.MinValue;
                            lastModified = _page.ModifiedDate;
                            if (lastModified != DateTime.MinValue)
                            {
                                StoreMatch(pageContent, match.Value, lastModified.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was created.
                    case "created":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            DateTime createdDate = DateTime.MinValue;
                            createdDate = _page.CreatedDate;
                            if (createdDate != DateTime.MinValue)
                            {
                                StoreMatch(pageContent, match.Value, createdDate.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page.
                    case "name":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }
                            StoreMatch(pageContent, match.Value, _page.Name);
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page in title form.
                    case "title":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }
                            StoreMatch(pageContent, match.Value, $"<h1>{_page.Name}</h1>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Inserts empty lines into the page.
                    case "br":
                    case "nl":
                    case "newline": //##NewLine([optional:default=1]count)
                        {
                            int count = 1;

                            if (args.Count > 1)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            if (args.Count > 0)
                            {
                                count = int.Parse(args[0]);
                            }

                            for (int i = 0; i < count; i++)
                            {
                                StoreMatch(pageContent, match.Value, $"<br />");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the navigation text for the current page.
                    case "navigation":
                        {
                            string navigation = string.Empty;

                            navigation = _page.Navigation;

                            if (navigation != string.Empty)
                            {
                                StoreMatch(pageContent, match.Value, navigation);
                            }
                        }
                        break;
                        //------------------------------------------------------------------------------------------------------------------------------                
                }
            }
        }

        /// <summary>
        /// These are functions that must be called after all other functions. For example, we can't build a table-of-contents until we have parsed the entire page.
        /// </summary>
        private void TransformPostProcess(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"(\#\#.*?\(.*?\))|(\#\#.+?\(\))|(\#\#\w+)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(pageContent.ToString());

            foreach (Match match in matches)
            {
                string keyword = string.Empty;
                var args = new List<string>(); ;

                MatchCollection rawargs = (new Regex(@"\(+?\)|\(.+?\)")).Matches(match.Value);
                if (rawargs.Count > 0)
                {
                    keyword = match.Value.Substring(2, match.Value.IndexOf('(') - 2).ToLower();

                    foreach (var rawarg in rawargs)
                    {
                        string rawArgTrimmed = rawarg.ToString().Substring(1, rawarg.ToString().Length - 2);
                        args.AddRange(rawArgTrimmed.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 2).ToLower(); ; //The match has no parameter.
                }

                switch (keyword)
                {
                    //Displays a tag link list.
                    case "tags": //##tags([optional:default=orderedlist]Format=OrderedList|FlatList,[optional:default=links]Layout=Links|Text)
                        {
                            string format = "orderedlist";
                            string display = "links";

                            if (args.Count > 0) format = args[0].ToLower();
                            if (args.Count > 1) display = args[1].ToLower();

                            var html = new StringBuilder();

                            if (format == "orderedlist")
                            {
                                html.Append("<ul>");
                                foreach (var tag in _tags)
                                {
                                    if (display == "links")
                                    {
                                        html.Append($"<li><a href=\"/Wiki/Tag/{tag}\">{tag}</a>");
                                    }
                                    else if (display == "text")
                                    {
                                        html.Append($"<li>{tag}");
                                    }
                                }
                                html.Append("</ul>");
                            }
                            else if (format == "flatlist")
                            {
                                foreach (var tag in _tags)
                                {
                                    if (display == "links")
                                    {
                                        html.Append($"<a href=\"/Wiki/Tag/{tag}\">{tag}</a> ");
                                    }
                                    else if (display == "text")
                                    {
                                        html.Append($"{tag} ");
                                    }
                                }
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Diplays a table of contents for the page based on the header tags.
                    case "toc":
                        {

                            var html = new StringBuilder();

                            var tags = from t in _tocTags
                                       orderby t.StartingPosition
                                       select t;

                            int currentLevel = 0;

                            foreach (var tag in tags)
                            {
                                if (tag.Level > currentLevel)
                                {
                                    while (currentLevel < tag.Level)
                                    {
                                        html.Append("<ul>");
                                        currentLevel++;
                                    }
                                }
                                else if (tag.Level < currentLevel)
                                {
                                    while (currentLevel > tag.Level)
                                    {

                                        html.Append("</ul>");
                                        currentLevel--;
                                    }
                                }

                                html.Append("<li><a href=\"#" + tag.HrefTag + "\">" + tag.Text + "</a></li>");
                            }

                            while (currentLevel > 0)
                            {
                                html.Append("</ul>");
                                currentLevel--;
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }

                        break;
                        //------------------------------------------------------------------------------------------------------------------------------                
                }
            }
        }

        #region Linq Getters.

        public Page GetPageFromPathInfo(string routeData)
        {
            routeData = HTML.CleanFullURI(routeData);
            routeData = routeData.Substring(1, routeData.Length - 2);

            var page = Repository.PageRepository.GetPageByNavigation(routeData);

            return page;
        }

        #endregion
    }
}
