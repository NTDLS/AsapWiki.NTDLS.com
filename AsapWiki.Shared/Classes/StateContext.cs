﻿using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using System.Collections.Generic;
using System.Linq;
using static AsapWiki.Shared.Constants;

namespace AsapWiki.Shared.Classes
{
    public class StateContext
    {
        public bool IsAuthenticated { get; set; }
        public User User { get; set; }
        public List<string> Roles { get; set; }
        public int? PageId { get; private set; } = null;
        public List<ProcessingInstruction> ProcessingInstructions { get; set; } = new List<ProcessingInstruction>();

        public void SetPageId(int? pageId)
        {
            PageId = pageId;
            if (pageId != null)
            {
                ProcessingInstructions = ProcessingInstructionRepository.GetPageProcessingInstructionsByPageId((int)pageId);
            }
            else
            {
                ProcessingInstructions = new List<ProcessingInstruction>();
            }
        }

        public bool IsPageLoaded => ((PageId ?? 0) > 0);
        public bool CanView => true;

        public bool CanEdit
        {
            get
            {
                if (IsAuthenticated)
                {
                    if (ProcessingInstructions.Where(o => o.Instruction.ToLower() == WikiInstruction.Protect.ToString().ToLower()).Any())
                    {
                        return (Roles.Contains(Constants.Roles.Administrator)
                            || Roles.Contains(Constants.Roles.Moderator));
                    }

                    return (Roles.Contains(Constants.Roles.Administrator)
                        || Roles.Contains(Constants.Roles.Contributor)
                        || Roles.Contains(Constants.Roles.Moderator));
                }

                return false;
            }
        }

        public bool CanCreate =>
            IsAuthenticated && CanEdit
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
                || Roles.Contains(Constants.Roles.Moderator));

        public bool CanDelete =>
            IsAuthenticated && CanCreate
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Moderator));
    }
}
