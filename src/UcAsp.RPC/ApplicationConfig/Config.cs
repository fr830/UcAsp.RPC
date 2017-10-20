namespace UcAsp.RPC
{
    using System;
    using System.IO;
    using System.Xml;

    public class Config : XmlBase
    {
        private const string SectionType =
            "System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null";

        // Fields
        private string _groupName = "profile";

        public Config()
        {
        }

        public Config(string fileName)
            : base(fileName)
        {
        }

        public Config(string fileName, string groupName)
            : base(fileName)
        {
            this._groupName = groupName;
        }

        public Config(Config config)
            : base(config)
        {
            this._groupName = config._groupName;
        }

        /// <summary>
        ///   Gets the default name for the Config file. 
        /// </summary>
        public override string DefaultName
        {
            get
            {
                return this.DefaultNameWithoutExtension + ".config";
            }
        }

        public string GroupName
        {
            get
            {
                return this._groupName;
            }

            set
            {
                this.VerifyNotReadOnly();
                if (this._groupName == value)
                {
                    return;
                }

                if (!this.RaiseChangeEvent(true, ProfileChangeType.Other, null, "GroupName", value))
                {
                    return;
                }

                this._groupName = value;
                if (this._groupName != null)
                {
                    this._groupName = this._groupName.Replace(' ', '_');

                    if (this._groupName.IndexOf(':') >= 0)
                    {
                        throw new Exception("GroupName may not contain a namespace prefix.");
                    }
                }

                this.RaiseChangeEvent(false, ProfileChangeType.Other, null, "GroupName", value);
            }
        }

        /// <summary>
        ///   Gets the name of the GroupName plus a slash or an empty string is HasGroupName is false. </summary>
        private string GroupNameSlash
        {
            get
            {
                return this.HasGroupName ? (this._groupName + "/") : string.Empty;
            }
        }

        /// <summary>
        ///   Gets whether we have a valid GroupName. </summary>
        private bool HasGroupName
        {
            get
            {
                return this._groupName != null && this._groupName != string.Empty;
            }
        }

        /// <summary>
        ///   Retrieves a copy of itself. 
        /// </summary>
        public override object Clone()
        {
            return new Config(this);
        }

        /// <summary>
        ///   Retrieves the names of all the entries inside a section. </summary>
        public override string[] GetEntryNames(string section)
        {
            // Verify the section exists
            if (!this.HasSection(section))
            {
                return null;
            }

            this.VerifyAndAdjustSection(ref section);
            XmlDocument doc = this.GetXmlDocument();
            XmlElement root = doc.DocumentElement;

            // Get the entry nodes
            XmlNodeList entryNodes = root.SelectNodes(this.GroupNameSlash + section + "/add[@key]");
            if (entryNodes == null)
            {
                return null;
            }

            // Add all entry names to the string array			
            string[] entries = new string[entryNodes.Count];
            int i = 0;

            foreach (XmlNode node in entryNodes)
            {
                entries[i++] = node.Attributes["key"].Value;
            }

            return entries;
        }

        public int GetGroupCount()
        {
            XmlDocument doc = this.GetXmlDocument();
            if (doc == null)
            {
                return 0;
            }

            XmlElement root = doc.DocumentElement;
            if (root == null)
            {
                return 0;
            }

            XmlNodeList note = root.SelectNodes(this._groupName);
            return note.Count;
        }

        /// <summary>
        ///   Retrieves the names of all the sections. </summary>
        public override string[] GetSectionNames()
        {
            // Verify the document exists
            XmlDocument doc = this.GetXmlDocument();
            if (doc == null)
            {
                return null;
            }

            // Get the root node, if it exists
            XmlElement root = doc.DocumentElement;
            if (root == null)
            {
                return null;
            }

            // Get the group node
            XmlNode groupNode = this.HasGroupName ? root.SelectSingleNode(this._groupName) : root;
            if (groupNode == null)
            {
                return null;
            }

            // Get the section nodes
            XmlNodeList sectionNodes = groupNode.ChildNodes;
            if (sectionNodes == null)
            {
                return null;
            }

            // Add all section names to the string array			
            string[] sections = new string[sectionNodes.Count];
            int i = 0;

            foreach (XmlNode node in sectionNodes)
            {
                sections[i++] = node.Name;
            }

            return sections;
        }

        /// <summary>
        ///   Retrieves the value of an entry inside a section. </summary>
        public override object GetValue(string section, string entry)
        {
            this.VerifyAndAdjustSection(ref section);
            this.VerifyAndAdjustEntry(ref entry);

            try
            {
                XmlDocument doc = this.GetXmlDocument();
                XmlElement root = doc.DocumentElement;

                XmlNode entryNode = root.SelectSingleNode(
                    this.GroupNameSlash + section + "/add[@key=\"" + entry + "\"]");
                return entryNode.Attributes["value"].Value;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public object GetValue(int i, string section, string entry)
        {
            this.VerifyAndAdjustSection(ref section);
            this.VerifyAndAdjustEntry(ref entry);

            try
            {
                XmlDocument doc = this.GetXmlDocument();
                XmlElement root = doc.DocumentElement;
                XmlNodeList list = root.SelectNodes(this._groupName);
                XmlNode entryNode = list[i].SelectSingleNode(string.Format("{0}/add[@key=\"{1}\"]", section, entry));
                if (entryNode == null)
                {
                    return null;
                }

                return entryNode.Attributes["value"].Value;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        ///   Removes an entry from a section. </summary>
        public override void RemoveEntry(string section, string entry)
        {
            this.VerifyNotReadOnly();
            this.VerifyAndAdjustSection(ref section);
            this.VerifyAndAdjustEntry(ref entry);

            // Verify the document exists
            XmlDocument doc = this.GetXmlDocument();
            if (doc == null)
            {
                return;
            }

            // Get the entry's node, if it exists
            XmlElement root = doc.DocumentElement;
            XmlNode entryNode = root.SelectSingleNode(this.GroupNameSlash + section + "/add[@key=\"" + entry + "\"]");
            if (entryNode == null)
            {
                return;
            }

            if (!this.RaiseChangeEvent(true, ProfileChangeType.RemoveEntry, section, entry, null))
            {
                return;
            }

            entryNode.ParentNode.RemoveChild(entryNode);
            this.Save(doc);
            this.RaiseChangeEvent(false, ProfileChangeType.RemoveEntry, section, entry, null);
        }

        /// <summary>
        ///   Removes a section. </summary>
        public override void RemoveSection(string section)
        {
            this.VerifyNotReadOnly();
            this.VerifyAndAdjustSection(ref section);

            // Verify the document exists
            XmlDocument doc = this.GetXmlDocument();
            if (doc == null)
            {
                return;
            }

            // Get the root node, if it exists
            XmlElement root = doc.DocumentElement;
            if (root == null)
            {
                return;
            }

            // Get the section's node, if it exists
            XmlNode sectionNode = root.SelectSingleNode(this.GroupNameSlash + section);
            if (sectionNode == null)
            {
                return;
            }

            if (!this.RaiseChangeEvent(true, ProfileChangeType.RemoveSection, section, null, null))
            {
                return;
            }

            sectionNode.ParentNode.RemoveChild(sectionNode);

            // Delete the configSections entry also			
            if (!this.IsAppSettings(section))
            {
                sectionNode =
                    root.SelectSingleNode(
                        "configSections/"
                        + (this.HasGroupName ? ("sectionGroup[@name=\"" + this._groupName + "\"]") : string.Empty)
                        + "/section[@name=\"" + section + "\"]");
                if (sectionNode == null)
                {
                    return;
                }

                sectionNode.ParentNode.RemoveChild(sectionNode);
            }

            this.Save(doc);
            this.RaiseChangeEvent(false, ProfileChangeType.RemoveSection, section, null, null);
        }

        /// <summary>
        ///   Sets the value for an entry inside a section. </summary>
        public override void SetValue(string section, string entry, object value)
        {
            // If the value is null, remove the entry
            if (value == null)
            {
                this.RemoveEntry(section, entry);
                return;
            }

            this.VerifyNotReadOnly();
            this.VerifyName();
            this.VerifyAndAdjustSection(ref section);
            this.VerifyAndAdjustEntry(ref entry);

            if (!this.RaiseChangeEvent(true, ProfileChangeType.SetValue, section, entry, value))
            {
                return;
            }

            bool hasGroupName = this.HasGroupName;
            bool isAppSettings = this.IsAppSettings(section);

            // If the file does not exist, use the writer to quickly create it
            if ((this._buffer == null || this._buffer.IsEmpty) && !File.Exists(this.Name))
            {
                XmlTextWriter writer = null;

                // If there's a buffer, write to it without creating the file
                if (this._buffer == null)
                {
                    writer = new XmlTextWriter(this.Name, this.Encoding);
                }
                else
                {
                    writer = new XmlTextWriter(new MemoryStream(), this.Encoding);
                }

                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument();

                writer.WriteStartElement("configuration");
                if (!isAppSettings)
                {
                    writer.WriteStartElement("configSections");
                    if (hasGroupName)
                    {
                        writer.WriteStartElement("sectionGroup");
                        writer.WriteAttributeString("name", null, this._groupName);
                    }

                    writer.WriteStartElement("section");
                    writer.WriteAttributeString("name", null, section);
                    writer.WriteAttributeString("type", null, SectionType);
                    writer.WriteEndElement();

                    if (hasGroupName)
                    {
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                if (hasGroupName)
                {
                    writer.WriteStartElement(this._groupName);
                }

                writer.WriteStartElement(section);
                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", null, entry);
                writer.WriteAttributeString("value", null, value.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();
                if (hasGroupName)
                {
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                if (this._buffer != null)
                {
                    this._buffer.Load(writer);
                }

                writer.Close();

                this.RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
                return;
            }

            // The file exists, edit it
            XmlDocument doc = this.GetXmlDocument();
            XmlElement root = doc.DocumentElement;

            XmlAttribute attribute = null;
            XmlNode sectionNode = null;

            // Check if we need to deal with the configSections element
            if (!isAppSettings)
            {
                // Get the configSections element and add it if it's not there
                XmlNode sectionsNode = root.SelectSingleNode("configSections");
                if (sectionsNode == null)
                {
                    sectionsNode = root.AppendChild(doc.CreateElement("configSections"));
                }

                XmlNode sectionGroupNode = sectionsNode;
                if (hasGroupName)
                {
                    // Get the sectionGroup element and add it if it's not there
                    sectionGroupNode = sectionsNode.SelectSingleNode("sectionGroup[@name=\"" + this._groupName + "\"]");
                    if (sectionGroupNode == null)
                    {
                        XmlElement element = doc.CreateElement("sectionGroup");
                        attribute = doc.CreateAttribute("name");
                        attribute.Value = this._groupName;
                        element.Attributes.Append(attribute);
                        sectionGroupNode = sectionsNode.AppendChild(element);
                    }
                }

                // Get the section element and add it if it's not there
                sectionNode = sectionGroupNode.SelectSingleNode("section[@name=\"" + section + "\"]");
                if (sectionNode == null)
                {
                    XmlElement element = doc.CreateElement("section");
                    attribute = doc.CreateAttribute("name");
                    attribute.Value = section;
                    element.Attributes.Append(attribute);

                    sectionNode = sectionGroupNode.AppendChild(element);
                }

                // Update the type attribute
                attribute = doc.CreateAttribute("type");
                attribute.Value = SectionType;
                sectionNode.Attributes.Append(attribute);
            }

            // Get the element with the sectionGroup name and add it if it's not there
            XmlNode groupNode = root;
            if (hasGroupName)
            {
                groupNode = root.SelectSingleNode(this._groupName);
                if (groupNode == null)
                {
                    groupNode = root.AppendChild(doc.CreateElement(this._groupName));
                }
            }

            // Get the element with the section name and add it if it's not there
            sectionNode = groupNode.SelectSingleNode(section);
            if (sectionNode == null)
            {
                sectionNode = groupNode.AppendChild(doc.CreateElement(section));
            }

            // Get the 'add' element and add it if it's not there
            XmlNode entryNode = sectionNode.SelectSingleNode("add[@key=\"" + entry + "\"]");
            if (entryNode == null)
            {
                XmlElement element = doc.CreateElement("add");
                attribute = doc.CreateAttribute("key");
                attribute.Value = entry;
                element.Attributes.Append(attribute);

                entryNode = sectionNode.AppendChild(element);
            }

            // Update the value attribute
            attribute = doc.CreateAttribute("value");
            attribute.Value = value.ToString();
            entryNode.Attributes.Append(attribute);

            // Save the file
            this.Save(doc);
            this.RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
        }

        /// <summary>
        ///   Verifies the given section name is not null and trims it. </summary>
        protected override void VerifyAndAdjustSection(ref string section)
        {
            base.VerifyAndAdjustSection(ref section);
            if (section.IndexOf(' ') >= 0)
            {
                section = section.Replace(' ', '_');
            }
        }

        /// <summary>
        ///   Retrieves whether we don't have a valid GroupName and a given section is 
        ///   equal to "appSettings". </summary>
        private bool IsAppSettings(string section)
        {
            return !this.HasGroupName && section != null && section == "appSettings";
        }
    }
}