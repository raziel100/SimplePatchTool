﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SimplePatchToolCore
{
	[XmlRoot( "ProjectInfo" )]
	public class ProjectInfo
	{
		internal const int LATEST_VERSION = 1;

		[XmlAttribute( AttributeName = "Surum" )]
		public int Version;

		private string m_name;
		public string Name
		{
			get { return m_name; }
			set
			{
				if( !PatchUtils.IsProjectNameValid( value ) )
					throw new FormatException( Localization.Get( StringId.E_XContainsInvalidCharacters, "'Name'" ) );

				m_name = value;
			}
		}

		public bool CreateRepairPatch;
		public bool CreateInstallerPatch;
		public bool CreateIncrementalPatch;
		[XmlElement( ElementName = "CreateIncrementalPatchesFromEachPreviousVersionToNewVersion" )]
		public bool CreateAllIncrementalPatches;

		public string BaseDownloadURL;
		public string MaintenanceCheckURL;
		public List<string> IgnoredPaths;
		public bool IsSelfPatchingApp;

		public ProjectInfo()
		{
			Version = LATEST_VERSION;
			Name = "NewProject";
			CreateRepairPatch = true;
			CreateInstallerPatch = true;
			CreateIncrementalPatch = true;
			CreateAllIncrementalPatches = false;
			BaseDownloadURL = "";
			MaintenanceCheckURL = "";
			IgnoredPaths = new List<string>() { "*" + PatchParameters.LOG_FILE_NAME };
			IsSelfPatchingApp = true;
		}
	}
}