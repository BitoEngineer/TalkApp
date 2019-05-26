using System;
using UnityEngine;

namespace Assets.Core.Extensions
{
    public static class MyUnityUtility
    {
        /// <summary>
        /// Call DontDestroyOnLoad for its GameObject if it's the first MonoBehaviour of this type and returns true.
        /// Otherwise, destroys its GameObject and returns false.
        /// </summary>
        /// <param name="script">MonoBehaviour</param>
        /// <returns>True if it's the first MonoBehaviour of this type. False if not and it was destroyed.</returns>
        public static bool DontDestroyNorCreateOnLoad(this MonoBehaviour script)
        {
            try
            {
                if (MonoBehaviour.FindObjectsOfType(script.GetType()).Length > 1)
                {
                    MonoBehaviour.Destroy(script.gameObject);
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message, script);
            }

            MonoBehaviour.DontDestroyOnLoad(script.gameObject);
            return true;
        }
    }
}
