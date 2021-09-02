using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.UI.Buss.Properties;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;

#endif


namespace Beamable.UI.Buss
{

    public abstract class StyleBehaviour : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Static Utility

        private static HashSet<string> _registeredTypeNames = new HashSet<string>();

        protected static void RegisterType<T>(string elementType) where T : StyleBehaviour
        {
            _registeredTypeNames.Add(elementType);
        }

        public static bool IsElementTypeName(string elementType, out string verifiedElementType)
        {
            if (_registeredTypeNames.Contains(elementType))
            {
                verifiedElementType = elementType;
                return true;
            }

            verifiedElementType = "unknown";
            return false;
        }
        #endregion

        public StyleObject InlineStyle;
        private StyleBundle _inlineStyleBundle;

        public StyleBundle InlineStyleBundle => _inlineStyleBundle ?? (_inlineStyleBundle = new StyleBundle
        {
            Rule = new SelectorWithStyle
            {
                Selector = new Selector
                {
                    InlineConstraint = this
                },
                Style = InlineStyle ?? (InlineStyle = new StyleObject())
            },
        });

        public List<StyleSheetObject> StyleSheets = new List<StyleSheetObject>();

        [SerializeField]
        private string _classString = "";
        private HashSet<string> _pseudoStates = new HashSet<string>();

        public string ClassString => _classString;

        public string Id => AsId(name);

        public string QualifiedSelectorString => $"{TypeString}#{Id}{ClassSelectorString}";
        public string ClassSelectorString => ClassString.Length > 0 ? $".{string.Join(".", GetClassNames())}" : "";

        public abstract string TypeString { get; }

        public Action OnStateUpdated;

        public abstract void Apply(StyleObject styles);

        public void OnPointerExit(PointerEventData eventData)
        {
            RemovePseudoState("hover");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AddPseudoState("hover");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (!gameObject.activeInHierarchy || !isActiveAndEnabled) return;

            ApplyStyleTree();
            base.OnValidate();
        }

#endif


        public static string AsId(string name)
        {
            return name
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "");
        }

        public HashSet<string> GetClassNames()
        {
            var set = new HashSet<string>();
            if (string.IsNullOrEmpty(ClassString)) return set;

            foreach (var className in ClassString.Split(' '))
            {
                set.Add(className.Trim().ToLower());
            }

            return set;
        }



        public StyleBehaviour GetParent()
        {
            if (!this || !transform) return null;

            var parent = transform.parent;
            if (parent == null || !parent)
            {
                return null;
            }

            return parent.GetComponent<StyleBehaviour>();
        }

        public StyleBehaviour GetRoot()
        {
            var root = this;
            while (true)
            {
                var nextRoot = root.GetParent();
                if (nextRoot == null)
                {
                    return root;
                }

                root = nextRoot;
            }

        }

        public List<StyleBehaviour> GetChildren()
        {
            var children = new List<StyleBehaviour>();

            void Search(Transform root)
            {
                for (var i = 0; i < root.childCount; i++)
                {
                    var child = root.GetChild(i);
                    var childStyle = child.GetComponent<StyleBehaviour>();

                    if (childStyle == null)
                    {
                        Search(child);
                    }
                    else
                    {
                        children.Add(childStyle);
                    }

                }
            }
            Search(transform);

//            for (var i = 0; i < transform.childCount; i++)
//            {
//                var child = transform.GetChild(i).GetComponent<StyleBehaviour>();
//                if (child != null)
//                {
//                    children.Add(child);
//                }
//                else
//                {
//
//                }
//            }

            return children;
        }



        public int MatchSelectorDistance(Selector selector)
        {

            /*
             * .header img#a
             *
             */

            var iter = selector.Ascend().GetEnumerator();
            var distance = 1;

            if (!iter.MoveNext())
            {
                // TODO: Should a selector with no clauses match? "" should match?
                return distance; // oh, there isn't any more. We're done!
            }
            var currentNode = this;
            while (true)
            {
                var matchesCurrent = iter.Current.Accept(currentNode);

                if (!matchesCurrent)
                {
                    // try the parent element. If the parent matches, then we should still be good.
                    currentNode = currentNode.GetParent();
                    if (currentNode == null)
                    {
                        // we reached the end-of-the line. Game over.
                        return 0;
                        //return false;
                    }
                    else
                    {
                        distance ++;
                        // simply try again.
                    }
                }
                else
                {

                    if (!iter.MoveNext())
                    {
                        return distance;
                        //return true; // oh, there isn't any more. We're done!
                    }
                    currentNode = currentNode.GetParent();
                    if (currentNode == null)
                    {
                        return 0;
                        //return false; // its the end of the line, mate.
                    }
                }
            }
        }


        public bool MatchesSelector(Selector selector)
        {
            return MatchSelectorDistance(selector) > 0;
        }

        /*
         * div
         *  span <----------
         *
         *
         * "span #foo div"
         */
        public bool Matches(Selector query)
        {
            var currentNode = this;
            foreach (var part in query.Ascend())
            {
                if (currentNode == null) return false;

                var matchCurrent = part.Accept(currentNode);

                if (!matchCurrent) return false;
                /*
                 * TODO:
                 * this function won't match correctly for parents that are higher up.
                 * For example...
                 * div#b
                 *   span
                 *     div#a
                 *
                 * with selector #b #a
                 *  then the div#a should be selectable...
                 */

                currentNode = currentNode.transform.parent?.GetComponent<StyleBehaviour>();
            }

            return true;
        }

        public virtual void ApplyStyleTree()
        {
            Apply(ComputeStyleObject());
        }

        public List<StyleBundle> GetApplicableStyles()
        {
            var styleSheets = GetStyleSheets().ToList();
            var output = new List<ComputeRuleBundle>();
            for (var i = 0; i < styleSheets.Count; i ++)
            {
                var sheet = styleSheets[i];
                var rules = sheet.Rules
                    .Where(rule => MatchesSelector(rule.Selector))
                    .Select(rule => new ComputeRuleBundle
                {
                    Data = new StyleBundle
                    {
                        Rule = rule,
                        Sheet = sheet
                    },
                    SheetDistance = (i+1)
                }).ToList();
                output.AddRange(rules);
            }

            var orderedRules = output
                .OrderByDescending(r => r.Data.Selector.WeightFrom(this) / r.SheetDistance)
                .Select(r => r.Data)
                .ToList();

            // need to include all inline styles from parents.
            var parentInlineStyles = new List<StyleBundle>();
            foreach (var parent in Climb())
            {
                var parentInline = parent.InlineStyleBundle;
                if (parentInline.Style.AnyDefinition)
                {
                    parentInlineStyles.Add(parent.InlineStyleBundle);
                }
            }

            foreach (var styleBundle in parentInlineStyles)
            {
                orderedRules.Insert(0, styleBundle);
            }
            orderedRules.Insert(0, InlineStyleBundle);


            return orderedRules;

        }

        public StyleObject ComputeStyleObject()
        {
            var output = GetApplicableStyles()
                .Select(x => x.Style)
                .Reverse()
                .Aggregate((agg, curr) => agg.Merge(curr));
            return output;
        }

        public struct ComputeRuleBundle
        {
            public StyleBundle Data;
            public float SheetDistance;
        }

        public void AddClass(string clazz)
        {
            var existing = GetClassNames();
            existing.Add(clazz);
            _classString = string.Join(" ", existing);
            RefreshStyles();
        }

        public void RemoveClass(string clazz)
        {
            var existing = GetClassNames();
            existing.Remove(clazz);
            _classString = string.Join(" ", existing);
            RefreshStyles();
        }

        public void SetClass(string clazz, bool value)
        {
            var existing = GetClassNames();
            if (existing.Contains(clazz) && !value)
            {
                RemoveClass(clazz);
            }

            if (!existing.Contains(clazz) && value)
            {
                AddClass(clazz);
            }
        }

        public void AddPseudoState(string state)
        {
            _pseudoStates.Add(state);
            RefreshStyles();
        }

        public void RemovePseudoState(string state)
        {
            _pseudoStates.Remove(state);
            RefreshStyles();
        }

        private void RefreshStyles()
        {
            ApplyStyleTreeChildren();
            OnStateUpdated?.Invoke();
        }

        public void ApplyStyleTreeChildren()
        {
            foreach (var elem in All())
            {
                elem.ApplyStyleTree();
            }
        }


        public void SetPseudoState(string state, bool value)
        {
            if (value)
            {
                AddPseudoState(state);
            }
            else
            {
                RemovePseudoState(state);
            }
        }

        public bool HasPseudoState(string state)
        {
            return _pseudoStates.Contains(state);
        }

        public StyleSheetObject GetFirstStyleSheet()
        {
            return GetStyleSheets().FirstOrDefault();
        }

        public IEnumerable<StyleSheetObject> GetStyleSheets()
        {
            // first, return our own sheets
            foreach (var sheet in StyleSheets)
            {
                yield return sheet;
            }

            // then, iterate over parent sheets.
            foreach (var parent in Climb())
            {
                foreach (var sheet in parent.StyleSheets)
                {
                    yield return sheet;
                }
            }

            // last, add in the default sheets.
            var instance = BussConfiguration.Instance;
            if (instance != null)
            {
                var enumerator = instance.EnumerateSheets();
                foreach (var sheet in enumerator)
                {
                    yield return sheet;
                }
            }

        }

        public IEnumerable<StyleBehaviour> Climb()
        {
            /*
             * Climb up the object tree until we reach the root of the entire world...
             * Starts with THE FIRST PARENT node, ends with ROOT node.
             */

            var curr = this;

            while (curr != null)
            {
                curr = curr.transform.parent?.GetComponent<StyleBehaviour>();
                if (curr == null)
                {
                    continue;
                }
                yield return curr;
            }
        }

        public IEnumerable<StyleBehaviour> All()
        {
            /*
             * Breath first search over entire element tree.
             */

            var toExpand = new Queue<StyleBehaviour>();
            var visited = new HashSet<StyleBehaviour>();
            toExpand.Enqueue(this);

            while (toExpand.Count > 0)
            {
                var current = toExpand.Dequeue();
                if (visited.Contains(current)) continue; // really, this could throw an error, because in a tree, we should never hit an element twice

                yield return current;

                visited.Add(current);

                for (var i = 0; i < current.transform.childCount; i++)
                {
                    var child = current.transform.GetChild(i).GetComponent<StyleBehaviour>();
                    if (child != null)
                    {
                        toExpand.Enqueue(child);
                    }
                }
            }

        }
//
//        public List<StyleBehaviour> QueryAll(Selector query)
//        {
//            return All().Where(e => e.Matches(query)).ToList();
//        }
//
//        public StyleBehaviour Query(Selector query)
//        {
//            foreach (var element in All())
//            {
//                if (element.Matches(query))
//                {
//                    return element;
//                }
//            }
//
//            return null;
//        }

//        public List<StyleBehaviour> QuerySelectorAll(Selector query)
//        {
//            return All().Where(e => e.MatchesSelector(query)).ToList();
//        }

    }
}
