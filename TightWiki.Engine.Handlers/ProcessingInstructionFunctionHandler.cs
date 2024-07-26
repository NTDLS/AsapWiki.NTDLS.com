﻿using System.Text;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.EngineFunction;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;
using static TightWiki.Library.Constants;

namespace TightWiki.Engine.Handlers
{
    public class ProcessingInstructionFunctionHandler : IFunctionHandler
    {
        private readonly Dictionary<string, int> _sequences = new();

        public FunctionPrototypeCollection Prototypes()
        {
            return ProcessingInstructionFunctionPrototypes.Collection;
        }

        public HandlerResult Handle(IWikifier wikifier, FunctionCall function, string scopeBody)
        {
            switch (function.Name.ToLower())
            {
                //We check wikifier.CurrentNestLevel here because we don't want to include the processing instructions on any parent pages that are injecting this one.

                //------------------------------------------------------------------------------------------------------------------------------
                case "systememojilist":
                    {
                        StringBuilder html = new();

                        html.Append($"<table class=\"table table-striped table-bordered \">");

                        html.Append($"<thead>");
                        html.Append($"<tr>");
                        html.Append($"<td><strong>Name</strong></td>");
                        html.Append($"<td><strong>Image</strong></td>");
                        html.Append($"<td><strong>Shortcut</strong></td>");
                        html.Append($"</tr>");
                        html.Append($"</thead>");

                        string category = wikifier.QueryString["Category"].ToString();

                        html.Append($"<tbody>");

                        if (string.IsNullOrWhiteSpace(category) == false)
                        {
                            var emojis = EmojiRepository.GetEmojisByCategory(category);

                            foreach (var emoji in emojis)
                            {
                                html.Append($"<tr>");
                                html.Append($"<td>{emoji.Name}</td>");
                                //html.Append($"<td><img src=\"/images/emoji/{emoji.Path}\" /></td>");
                                html.Append($"<td><img src=\"/File/Emoji/{emoji.Name.ToLower()}\" /></td>");
                                html.Append($"<td>{emoji.Shortcut}</td>");
                                html.Append($"</tr>");
                            }
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "systememojicategorylist":
                    {
                        var categories = EmojiRepository.GetEmojiCategoriesGrouped();

                        StringBuilder html = new();

                        html.Append($"<table class=\"table table-striped table-bordered \">");

                        int rowNumber = 0;

                        html.Append($"<thead>");
                        html.Append($"<tr>");
                        html.Append($"<td><strong>Name</strong></td>");
                        html.Append($"<td><strong>Count of Emojis</strong></td>");
                        html.Append($"</tr>");
                        html.Append($"</thead>");

                        foreach (var category in categories)
                        {
                            if (rowNumber == 1)
                            {
                                html.Append($"<tbody>");
                            }

                            html.Append($"<tr>");
                            html.Append($"<td><a href=\"/wiki_help::list_of_emojis_by_category?category={category.Category}\">{category.Category}</a></td>");
                            html.Append($"<td>{category.EmojiCount:N0}</td>");
                            html.Append($"</tr>");
                            rowNumber++;
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString())
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedDecode]
                        };
                    }
                //------------------------------------------------------------------------------------------------------------------------------
                case "hidefooterlastmodified":
                    {
                        wikifier.ProcessingInstructions.Add(WikiInstruction.HideFooterLastModified);

                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "hidefootercomments":
                    {
                        wikifier.ProcessingInstructions.Add(WikiInstruction.HideFooterComments);
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "nocache":
                    {
                        wikifier.ProcessingInstructions.Add(WikiInstruction.NoCache);
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "deprecate":
                    {
                        if (wikifier.CurrentNestLevel == 0)
                        {
                            wikifier.ProcessingInstructions.Add(WikiInstruction.Deprecate);
                            wikifier.Headers.Add("<div class=\"alert alert-danger\">This page has been deprecated and will eventually be deleted.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "protect":
                    {
                        if (wikifier.CurrentNestLevel == 0)
                        {
                            bool isSilent = function.Parameters.Get<bool>("isSilent");
                            wikifier.ProcessingInstructions.Add(WikiInstruction.Protect);
                            if (isSilent == false)
                            {
                                wikifier.Headers.Add("<div class=\"alert alert-info\">This page has been protected and can not be changed by non-moderators.</div>");
                            }
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "template":
                    {
                        if (wikifier.CurrentNestLevel == 0)
                        {
                            wikifier.ProcessingInstructions.Add(WikiInstruction.Template);
                            wikifier.Headers.Add("<div class=\"alert alert-secondary\">This page is a template and will not appear in indexes or glossaries.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "review":
                    {
                        if (wikifier.CurrentNestLevel == 0)
                        {
                            wikifier.ProcessingInstructions.Add(WikiInstruction.Review);
                            wikifier.Headers.Add("<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "include":
                    {
                        if (wikifier.CurrentNestLevel == 0)
                        {
                            wikifier.ProcessingInstructions.Add(WikiInstruction.Include);
                            wikifier.Headers.Add("<div class=\"alert alert-secondary\">This page is an include and will not appear in indexes or glossaries.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "draft":
                    {
                        if (wikifier.CurrentNestLevel == 0)
                        {
                            wikifier.ProcessingInstructions.Add(WikiInstruction.Draft);
                            wikifier.Headers.Add("<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.KillTrailingLine]
                        };
                    }
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}