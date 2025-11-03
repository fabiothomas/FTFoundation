using UnityEngine;
using UnityEngine.Events;

namespace Scripts.References.Interfaces
{
    public interface IReferenceService
    {
        public void RegisterReference(MonoBehaviour ref_);
        public void UnregisterReference(MonoBehaviour ref_);
        public T GetReference<T>() where T : MonoBehaviour;
        public void UseReference<T>(UnityAction<T> action, UnityAction fallback) where T : MonoBehaviour;
    }
}