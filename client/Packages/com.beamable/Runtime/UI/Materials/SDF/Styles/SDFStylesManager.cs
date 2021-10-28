using System.Collections.Generic;
using Beamable.UI.SDF;
using UnityEngine;

namespace UI.Materials.SDF.Styles
{
    public class SDFStylesManager
    {
        #region Singleton implementation

        private static readonly object _padlock = new object();
        private static SDFStylesManager _instance;

        public static SDFStylesManager Instance
        {
            get
            {
                lock (_padlock)
                {
                    return _instance = _instance ?? new SDFStylesManager();
                }
            }
        }

        #endregion

        private SDFStylesManager()
        {
        }

        private void Refresh()
        {
        }
    }
}