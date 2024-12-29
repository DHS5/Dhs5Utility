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
        /// <summary>
        /// Registers the Gameplay Tags from <paramref name="tagsList"/>
        /// </summary>
        /// <remarks>
        /// If <paramref name="component"/>'s game object has already registered tags, <paramref name="tagsList"/> will override the former ones
        /// </remarks>
        public static void Register(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return;

            Register(component.gameObject, tagsList);
        }

        /// <summary>
        /// Unregisters the Gameplay Tags associated with <paramref name="go"/>
        /// </summary>
        public static void Unregister(GameObject go)
        {
            if (go == null) return;

            _tags.Remove(go.GetInstanceID());
        }
        /// <summary>
        /// Unregisters the Gameplay Tags associated with <paramref name="component"/>'s game object
        /// </summary>
        public static void Unregister(Component component)
        {
            if (component == null) return;

            Unregister(component.gameObject);
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

        public static void Get_NoAlloc(GameObject go, GameplayTagsList tagsList)
        {
            if (go == null || !_tags.TryGetValue(go.GetInstanceID(), out var tags)) return;

            tagsList.Set(tags);
        }
        public static void Get_NoAlloc(Component component, GameplayTagsList tagsList)
        {
            if (component == null) return;

            Get_NoAlloc(component.gameObject, tagsList);
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

    public static class GameplayTagsExtension
    {
        #region Registration

        /// <inheritdoc cref="GameplayTags.Register(GameObject, GameplayTagsList)"/>
        public static void RegisterGameplayTags(this GameObject gameObject, GameplayTagsList tagsList)
        {
            GameplayTags.Register(gameObject, tagsList);
        }
        /// <inheritdoc cref="GameplayTags.Register(Component, GameplayTagsList)"/>
        public static void RegisterGameplayTags(this Component component, GameplayTagsList tagsList)
        {
            GameplayTags.Register(component, tagsList);
        }

        /// <inheritdoc cref="GameplayTags.Unregister(GameObject)"/>
        public static void UnregisterGameplayTags(this GameObject gameObject)
        {
            GameplayTags.Unregister(gameObject);
        }
        /// <inheritdoc cref="GameplayTags.Unregister(Component)"/>
        public static void UnregisterGameplayTags(this Component component)
        {
            GameplayTags.Unregister(component);
        }

        #endregion

        #region Get

        public static GameplayTagsList GetGameplayTags(this GameObject gameObject)
        {
            return GameplayTags.Get(gameObject);
        }
        public static GameplayTagsList GetGameplayTags(this Component component)
        {
            return GameplayTags.Get(component);
        }

        #endregion


        #region ADD

        public static void AddGameplayTags(this GameObject gameObject, GameplayTagsList tagsList)
        {
            GameplayTags.AddTags(gameObject, tagsList);
        }
        public static void AddGameplayTags(this Component component, GameplayTagsList tagsList)
        {
            GameplayTags.AddTags(component, tagsList);
        }

        #endregion

        #region REMOVE

        public static void RemoveGameplayTags(this GameObject gameObject, GameplayTagsList tagsList)
        {
            GameplayTags.RemoveTags(gameObject, tagsList);
        }
        public static void RemoveGameplayTags(this Component component, GameplayTagsList tagsList)
        {
            GameplayTags.RemoveTags(component, tagsList);
        }

        #endregion


        #region Contains

        public static bool ContainsGameplayTags(this GameObject gameObject, GameplayTagsList tagsList)
        {
            return GameplayTags.Contains(gameObject, tagsList);
        }
        public static bool ContainsGameplayTags(this Component component, GameplayTagsList tagsList)
        {
            return GameplayTags.Contains(component, tagsList);
        }
        
        public static bool ContainsAnyGameplayTags(this GameObject gameObject, GameplayTagsList tagsList)
        {
            return GameplayTags.ContainsAny(gameObject, tagsList);
        }
        public static bool ContainsAnyGameplayTags(this Component component, GameplayTagsList tagsList)
        {
            return GameplayTags.ContainsAny(component, tagsList);
        }

        #endregion
    }
}
