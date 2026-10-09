using System.Collections;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public interface ILoadable
    {
        public IEnumerator LoadCoroutine(ESaveCategory category, uint iteration, BaseSaveSubObject subObject);

        public bool CanLoad(ESaveCategory category, uint iteration);

        /// <summary>
        /// Called instead of <see cref="LoadCoroutine"/> when the loaded save has no data for <paramref name="category"/>
        /// (save made before the category existed, new game...) : reset to the default state here.<br></br>
        /// Called once per load, when <paramref name="category"/> first comes up in the load order, regardless of <see cref="CanLoad"/>.
        /// </summary>
        public void LoadDefault(ESaveCategory category);
    }
}
