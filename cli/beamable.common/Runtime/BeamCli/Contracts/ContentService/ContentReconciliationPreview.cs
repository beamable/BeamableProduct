using System;

namespace Beamable.Common.BeamCli.Contracts
{
    [CliContractType, Serializable]
    public class ContentReconciliationPreview
    {
        public string ContentId;
        public string ManifestUid;
        /// <summary>Opaque review token binding scope, local file revision, and reviewed realm values.</summary>
        public string LocalRevision;
        public bool LocalExists;
        public bool RealmExists;
        /// <summary>Properties and tags only. Null when this side is absent.</summary>
        public string LocalJson;
        public string RealmJson;
        public ContentReconciliationDifference[] Differences;
    }

    [CliContractType, Serializable]
    public class ContentReconciliationDifference
    {
        public string Property;
        public string LocalValue;
        public string RealmValue;
    }
}
