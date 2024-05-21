﻿using TightWiki.DataModels;

namespace TightWiki.ViewModels.Admin
{
    public class EmojiViewModel : ViewModelBase
    {
        public Emoji Emoji { get; set; } = new();
        public string OriginalName { get; set; } = string.Empty;
        public string Categories { get; set; } = string.Empty;
    }
}
