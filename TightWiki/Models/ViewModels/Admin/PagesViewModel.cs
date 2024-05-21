﻿namespace TightWiki.Models.ViewModels.Admin
{
    public class PagesViewModel : ViewModelBase
    {
        public List<DataModels.Page> Pages { get; set; } = new();

        public string SearchTokens { get; set; } = string.Empty;
    }
}
