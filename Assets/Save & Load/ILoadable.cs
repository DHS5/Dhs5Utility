using System.Collections;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public interface ILoadable
    {
        public IEnumerator LoadCoroutine(ESaveCategory category, uint iteration, BaseSaveSubObject subObject);

        public bool CanLoad(ESaveCategory category, uint iteration);
    }
}
