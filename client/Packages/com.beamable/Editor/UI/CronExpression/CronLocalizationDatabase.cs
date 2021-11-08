using System.Collections.Generic;
using UnityEngine;

namespace Beamable.CronExpression
{
    public class CronLocalizationDatabase : ScriptableObject
    {
        public CronLocale DefaultLocalization => defaultLocalization;
        public List<CronLocalizationData> SupportedLocalizations => supportedLocalizations;

        [SerializeField] private CronLocale defaultLocalization;
        [SerializeField] private List<CronLocalizationData> supportedLocalizations;
    }

    public enum CronLocale
    {
        en_US,
        pl_PL
    }
}
