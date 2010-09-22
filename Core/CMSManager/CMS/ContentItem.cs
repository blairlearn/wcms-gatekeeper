﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
namespace GKManagers.CMSManager.CMS
{
    public class ContentItem
    {
        //Constructor for updating  content Item
        public ContentItem(long id,Dictionary<string,string> fields,string targetFolder)
        {
            ID = id;
            Fields = fields;
            TargetFolder = targetFolder;
        }

        //constructor for creating new content item
        public ContentItem(Dictionary<string, string> fields, string targetFolder)
        {            
            Fields = fields;
            TargetFolder = targetFolder;
        }

        public long ID {get;private set;}
        public Dictionary<string, string> Fields { get; private set; }
        public string TargetFolder { get; private set; }

    }
}
