﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SharpWiki.Shared.Library
{
    public class CountryItem
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public static List<CountryItem> GetAll()
        {
            var list = new List<CountryItem>();

            var cultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (var culture in cultureInfo)
            {
                var regionInfo = new RegionInfo(culture.LCID);

                if (list.Where(o => o.Value == regionInfo.Name).Any() == false)
                {
                    list.Add(new CountryItem
                    {
                        Text = regionInfo.DisplayName,
                        Value = regionInfo.Name
                    });
                }
            }

            return list;
        }
    }
}