﻿using TightWiki.Shared.Models.Data;
using System.Collections.Generic;

namespace TightWiki.Shared.Models.View
{
    public class FileAttachmentModel : ModelBase
    {
        public List<PageFileAttachment> Files { get; set; } = new List<PageFileAttachment>();
    }
}
