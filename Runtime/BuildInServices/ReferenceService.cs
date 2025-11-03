using System.Collections.Generic;
using Scripts.Foundation.Attributes;
using Scripts.References.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts.Services.FoundationServices
{
    [Service(typeof(IReferenceService), ServiceType.SINGLETON)]
    public class ReferenceService : IReferenceService
    {
        ILoggerService _ls;
        void Inject(ILoggerService ls)
        {
            _ls = ls;
        }

        private readonly Dictionary<string, MonoBehaviour> referenceList = new();

        /// <summary>
        /// Registers a reference. This method should be called when a reference is created and before it's requested by any other object likely in the reference's own <c>Awake()</c> method.
        /// </summary>
        /// <param name="ref_">Reference to be registered</param>
        public void RegisterReference(MonoBehaviour ref_)
        {
            if (!ref_)
            {
                _ls.LogWarning("Could not register object because no reference was given");
                return;
            }

            string name = ref_.GetType().Name;
            if (referenceList.ContainsKey(name))
            {
                _ls.LogError($"{name} is already assigned and can not be assigned multiple times. Please ensure there is only one {name} in this scene");
                return;
            }

            referenceList.Add(name, ref_);
        }

        /// <summary>
        /// Unregisters a reference. This method should be called when the reference is dostroyed likeley in the reference's own <c>OnDestroy()</c> method.
        /// </summary>
        /// <param name="ref_">Reference to be unregistered</param>
        /// <remarks>This is important because if the reference isn't unregistered properly it may leave behind artifacts. </remarks>
        public void UnregisterReference(MonoBehaviour ref_)
        {
            if (!ref_)
            {
                _ls.LogWarning("Could not unregister object because no reference was given");
                return;
            }

            string name = ref_.GetType().Name;
            if (!referenceList.ContainsKey(name))
            {
                _ls.LogWarning($"{name} cannot be unassigned because it has not been assigned in the first place");
                return;
            }

            referenceList.Remove(name);
        }

        /// <summary>
        /// Finds the reference defined by the given generic type and returns it. <br/>
        /// This method should only be used when you're certain the reference exists. <br/>
        /// If there is a chance the reference does not exist use <c>UseReference()</c> instead. <br/>
        /// If this method is called and the reference does not exist <c>default</c> is returned instead
        /// </summary>
        /// <remarks> Never use this method in <c>Awake()</c> or in <c>OnDestroy()</c> </remarks>
        public T GetReference<T>() where T : MonoBehaviour
        {
            string name = typeof(T).Name;
            if (referenceList.ContainsKey(name) && referenceList[name] is T t) return t;
            _ls.LogWarning($"Object '{name}' could not be found. Make sure to add this object to the scene exactly once");
            return default;
        }

        /// <summary>
        /// Finds the reference defined by the given generic type and executes the given action with it.
        /// </summary>
        /// <param name="action"> the action to perform with the found reference. </param>
        /// <param name="fallback"> the fallback action in case the reference is not found. </param>
        /// <remarks> Never use this method in <c>Awake()</c> or in <c>OnDestroy()</c> </remarks>
        public void UseReference<T>(UnityAction<T> action, UnityAction fallback) where T : MonoBehaviour
        {
            string name = typeof(T).Name;
            if (referenceList.ContainsKey(name) && referenceList[name] is T t) action(t);
            else fallback();
        }
    }
}