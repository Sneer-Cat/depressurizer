﻿/*
    This file is part of Depressurizer.
    Original work Copyright 2011, 2012, 2013 Steve Labbe.
    Modified work Copyright 2017 Martijn Vegter.

    Depressurizer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Depressurizer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Depressurizer.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Xml;
using Depressurizer.Lib;

namespace Depressurizer.DBEdit
{
    internal class FetchPrcDlg : CancelableDlg
    {
        public int Added { get; private set; }
        private XmlDocument doc;

        public FetchPrcDlg() : base(GlobalStrings.CDlgFetch_UpdatingGameList, false)
        {
            SetText(GlobalStrings.CDlgFetch_DownloadingGameList);
            Added = 0;
        }

        protected override void RunProcess()
        {
            Added = 0;
            doc = GameDB.FetchAppListFromWeb();
            OnThreadCompletion();
        }

        protected override void Finish()
        {
            if (!Canceled && (doc != null) && (Error == null))
            {
                SetText(GlobalStrings.CDlgFetch_FinishingDownload);
                Added = Program.GameDB.IntegrateAppList(doc);
                OnJobCompletion();
            }
        }
    }
}