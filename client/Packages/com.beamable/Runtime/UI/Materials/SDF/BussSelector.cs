using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Graphs;

namespace Beamable.UI.BUSS {
    public abstract class BussSelector {
        public abstract bool CheckMatch(BUSSElement bussElement);
    }

    public class UniversalSelector : BussSelector {
        
        public static UniversalSelector Get { get; } = new UniversalSelector();

        private UniversalSelector() { }

        public override bool CheckMatch(BUSSElement bussElement) {
            return bussElement != null;
        }
    }

    public class IdSelector : BussSelector {
        public readonly string id;
        
        private IdSelector(string id) {
            this.id = id;
        }
        
        private static Dictionary<string, IdSelector> _idSelectors = new Dictionary<string, IdSelector>();

        public static IdSelector Get(string id) {
            if (!_idSelectors.TryGetValue(id, out var selector)) {
                _idSelectors[id] = selector = new IdSelector(id);
            }

            return selector;
        }
        
        public override bool CheckMatch(BUSSElement bussElement) {
            if (bussElement == null) return false;
            return bussElement.Id == id;
        }
    }
    
    public class ClassSelector : BussSelector {
        public readonly string className;
        
        private ClassSelector(string className) {
            this.className = className;
        }

        private static Dictionary<string, ClassSelector> _classSelectors = new Dictionary<string, ClassSelector>();
        
        public static ClassSelector Get(string className) {
            if (!_classSelectors.TryGetValue(className, out var selector)) {
                _classSelectors[className] = selector = new ClassSelector(className);
            }

            return selector;
        }

        public override bool CheckMatch(BUSSElement bussElement) {
            if (bussElement == null) return false;
            return bussElement.Classes.Contains(className);
        }
    }

    public class CombinedSelector : BussSelector {
        public readonly BussSelector[] selectors;
        public readonly bool requireAll;
        
        public CombinedSelector(BussSelector[] selectors, bool requireAll) {
            this.selectors = selectors;
            this.requireAll = requireAll;
        }

        public override bool CheckMatch(BUSSElement bussElement) {
            if (bussElement == null) return false;
            if (requireAll) {
                foreach (var selector in selectors) {
                    if (!selector.CheckMatch(bussElement)) {
                        return false;
                    }
                }

                return true;
            }
            else {
                foreach (var selector in selectors) {
                    if (selector.CheckMatch(bussElement)) {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class ParentedSelector : BussSelector {
        public readonly BussSelector baseSelector;
        public readonly BussSelector parentSelector;
        public readonly bool onlyDirectParent;
        
        public ParentedSelector(BussSelector baseSelector, BussSelector parentSelector, bool onlyDirectParent) {
            this.parentSelector = parentSelector;
            this.onlyDirectParent = onlyDirectParent;
            this.baseSelector = baseSelector;
        }

        public override bool CheckMatch(BUSSElement bussElement) {
            if (bussElement == null) return false;
            if (baseSelector.CheckMatch(bussElement)) {
                if (onlyDirectParent) {
                    return parentSelector.CheckMatch(bussElement.Parent);
                }
                else {
                    while (bussElement.Parent != null) {
                        bussElement = bussElement.Parent;
                        if (parentSelector.CheckMatch(bussElement)) {
                            return true;
                        }

                    }
                }
            }

            return false;
        }
    }

    public static class BussSelectorParser {
        public static readonly Regex IdRegex = new Regex("#[a-zA-Z0-9-_]+");
        public static readonly Regex ClassRegex = new Regex("\\.[a-zA-Z0-9-_]+");
        public static readonly Regex TypeRegex = new Regex("^[a-zA-Z0-9_]+");
        
        public static BussSelector Parse(string input) {
            var separation = input.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
            var selectors = new List<BussSelector>();
            foreach (var part in separation) {
                var selector = TryParseSingle(part);
                if (selector != null) {
                    selectors.Add(selector);
                }
            }

            if (selectors.Count > 1) {
                return new CombinedSelector(selectors.ToArray(), false);
            }
            else if (selectors.Count > 0) {
                return selectors[0];
            }

            return null;
        }
    
        private static BussSelector TryParseSingle(string input) {
            var separation = input.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
            BussSelector parent = null;
            bool onlyDirectParenting = false;
            for (int i = 0; i < separation.Length; i++) {
                if (separation[i] == ">") {
                    onlyDirectParenting = true;
                    continue;
                }
                var selector = TryParseSimple(separation[i]);
                if (selector != null) {
                    if (parent == null) {
                        parent = selector;
                    }
                    else {
                        parent = new ParentedSelector(selector, parent, onlyDirectParenting);
                    }

                    onlyDirectParenting = false;
                }
            }

            return parent;
        }

        private static BussSelector TryParseSimple(string input) {

            if (input.Trim() == "*") {
                return UniversalSelector.Get;
            }
            
            var selectors = new List<BussSelector>();
            var idMatch = IdRegex.Match(input);
            if (idMatch.Success) {
                selectors.Add(IdSelector.Get(idMatch.Value.Substring(1)));
            }

            var classMatches = ClassRegex.Matches(input);
            for (int i = 0; i < classMatches.Count; i++) {
                var match = classMatches[i];
                selectors.Add(ClassSelector.Get(match.Value.Substring(1)));
            }

            if (selectors.Count > 1) {
                return new CombinedSelector(selectors.ToArray(), true);
            }
            else if (selectors.Count > 0) {
                return selectors[0];
            }

            return null;
        }
    }
}