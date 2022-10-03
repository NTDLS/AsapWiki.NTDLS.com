﻿using System.Web.Mvc;

namespace AsapWiki.Shared.Models.View
{
    public class EditPageModel
    {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Navigation { get; set; }
		public string Description { get; set; }
		[AllowHtml]
		public string Body { get; set; }
	}
}