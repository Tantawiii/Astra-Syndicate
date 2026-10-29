using UnityEngine;

namespace AstraSyndicate.Save
{
    /// <summary>
    /// Asset-based factory for an <see cref="ISaveDataProtector"/>. The encryption module subclasses
    /// this, creates an asset of its subclass and assigns it in GameConfig; the game code does not
    /// need to change.
    /// </summary>
    public abstract class SaveProtectorProvider : ScriptableObject
    {
        public abstract ISaveDataProtector CreateProtector();
    }
}
