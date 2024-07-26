﻿namespace TightWiki.Engine.Library
{
    //Table of contents tag.
    public class TOCTag
    {
        public int Level;
        public string HrefTag;
        public string Text;
        public int StartingPosition;

        public TOCTag(int level, int startingPosition, string hrefTag, string text)
        {
            Level = level;
            StartingPosition = startingPosition;
            HrefTag = hrefTag;
            Text = text;
        }
    }
}