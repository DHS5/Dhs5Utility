using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public interface ISaveParameter
    {
        public string GetExtension();
        public System.Text.Encoding GetEncoding();
    }
}
