using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Tags
{
    public static class GameplayTags
    {
        #region Members

        private static Dictionary<int, HashSet<int>> _tags = new();

        #endregion

        #region Registration

        /// <summary>
        /// Registers the Gameplay Tags from <paramref name="tagsList"/>
        /// </summary>
        /// <remarks>
        /// If <paramref name="go"/> has already registered tags, <paramref name="tagsList"/> will override the former ones
        /// </remarks>
        public static void Register(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !tagsList.IsValid()) return;

            HashSet<int> tags = new();
            foreach (var tag in tagsList)
            {
                tags.Add(tag);
            }
            _tags[go.GetInstanceID()] = tags;
        }
        /// <inheritdoc cref="Register(GameObject, GameplayTagsList)"/>
        public static void Register(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return;

            Register(component.gameObject, tagsList);
        }

        public static void Unregister(GameObject go)
        {
            if (go == null) return;

            _tags.Remove(go.GetInstanceID());
        }
        public static void Unregister(Component component)
        {
            if (component == null) return;

            Unregister(component.gameObject);
        }

        #endregion


        #region ADD

        public static void AddTags(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !tagsList.IsValid()) return;

            // Add
            if (_tags.TryGetValue(go.GetInstanceID(), out var currentTags))
            {
                foreach (var tag in tagsList)
                {
                    currentTags.Add(tag);
                }
            }
            // If not registered yet, simply register
            else
            {
                Register(go, tagsList);
            }
        }
        public static void AddTags(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return;

            AddTags(component.gameObject, tagsList);
        }

        #endregion
        
        #region REMOVE

        public static void RemoveTags(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !tagsList.IsValid()) return;

            // Remove
            if (_tags.TryGetValue(go.GetInstanceID(), out var currentTags))
            {
                foreach (var tag in tagsList)
                {
                    currentTags.Remove(tag);
                }
            }
        }
        public static void RemoveTags(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return;

            RemoveTags(component.gameObject, tagsList);
        }

        #endregion


        #region Get

        public static GameplayTagsList Get(GameObject go)
        {
            if (go == null || !_tags.TryGetValue(go.GetInstanceID(), out var tags)) return null;

            return new GameplayTagsList(tags);
        }
        public static GameplayTagsList Get(Component component)
        {
            if (component == null) return null;

            return Get(component.gameObject);
        }

        #endregion

        #region Contains

        public static bool Contains(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !tagsList.IsValid() || !_tags.TryGetValue(go.GetInstanceID(), out var currentTags)) return false;

            foreach (var tag in tagsList)
            {
                if (!currentTags.Contains(tag)) return false;
            }
            return true;
        }
        public static bool Contains(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return false;

            return Contains(component.gameObject, tagsList);
        }
        
        public static bool ContainsAny(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !tagsList.IsValid() || !_tags.TryGetValue(go.GetInstanceID(), out var currentTags)) return false;

            foreach (var tag in tagsList)
            {
                if (currentTags.Contains(tag)) return true;
            }
            return false;
        }
        public static bool ContainsAny(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return false;

            return ContainsAny(component.gameObject, tagsList);
        }

        #endregion

        #region Union

        public static GameplayTagsList Union(GameObject go1, GameObject go2)
        {
            if (go1 == null || !_tags.TryGetValue(go1.GetInstanceID(), out var go1Tags))
            {
                return Get(go2);
            }
            if (go2 == null || !_tags.TryGetValue(go2.GetInstanceID(), out var go2Tags))
            {
                return Get(go1);
            }

            HashSet<int> union = new();

            foreach (var tag in go1Tags)
                union.Add(tag);
            
            foreach (var tag in go2Tags)
                union.Add(tag);

            return new GameplayTagsList(union);
        }
        public static GameplayTagsList Union(Component comp1, Component comp2)
        {
            if (comp1 == null) return Get(comp2);
            if (comp2 == null) return Get(comp1);

            return Union(comp1.gameObject, comp2.gameObject);
        }
        public static GameplayTagsList Union(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !_tags.TryGetValue(go.GetInstanceID(), out var goTags))
            {
                return tagsList;
            }
            if (!tagsList.IsValid())
            {
                return Get(go);
            }

            HashSet<int> union = new();

            foreach (var tag in goTags)
                union.Add(tag);

            foreach (var tag in tagsList)
                union.Add(tag);

            return new GameplayTagsList(union);
        }
        public static GameplayTagsList Union(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return tagsList;

            return Union(component.gameObject, tagsList);
        }

        #endregion

        #region Intersection

        public static GameplayTagsList Intersection(GameObject go1, GameObject go2)
        {
            if (go1 == null || !_tags.TryGetValue(go1.GetInstanceID(), out var go1Tags) ||
                go2 == null || !_tags.TryGetValue(go2.GetInstanceID(), out var go2Tags))
            {
                return null;
            }

            HashSet<int> intersection = new();

            foreach (var tag in go1Tags)
            {
                if (go2Tags.Contains(tag))
                {
                    intersection.Add(tag);
                }
            }

            return new GameplayTagsList(intersection);
        }
        public static GameplayTagsList Intersection(Component comp1, Component comp2)
        {
            if (comp1 == null) return null; 
            if (comp2 == null) return null;

            return Intersection(comp1.gameObject, comp2.gameObject);
        }
        public static GameplayTagsList Intersection(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !_tags.TryGetValue(go.GetInstanceID(), out var goTags) 
                || !tagsList.IsValid())
            {
                return null;
            }

            HashSet<int> intersection = new();

            foreach (var tag in goTags)
            {
                if (tagsList.Contains(tag))
                {
                    intersection.Add(tag);
                }
            }

            return new GameplayTagsList(intersection);
        }
        public static GameplayTagsList Intersection(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return null;

            return Intersection(component.gameObject, tagsList);
        }

        #endregion
    }
}
