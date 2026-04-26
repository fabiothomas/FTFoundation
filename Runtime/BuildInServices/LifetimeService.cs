using System;
using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(ILifetimeService), ServiceType.SINGLETON)]
    public class LifetimeService : ILifetimeService
    {
        private readonly List<Action> updateActions = new();
        private readonly List<Action> fixedUpdateActions = new();
        private readonly List<Action> lateUpdateActions = new();
        void Inject(IDedicatedObjectService dedicatedObjectService)
        {
            GameObject obj = dedicatedObjectService.Get();
            LifetimeServiceHelper helper = obj.AddComponent<LifetimeServiceHelper>();
            helper.Initialize(this);
        }

        public IDisposable OnUpdate(Action action)
        {
            updateActions.Add(action);
            return new DelegateDisposable(() => updateActions.Remove(action));
        }

        public IDisposable OnFixedUpdate(Action action)
        {
            fixedUpdateActions.Add(action);
            return new DelegateDisposable(() => fixedUpdateActions.Remove(action));
        }

        public IDisposable OnLateUpdate(Action action)
        {
            lateUpdateActions.Add(action);
            return new DelegateDisposable(() => lateUpdateActions.Remove(action));
        }

        private void Update()
        {
            foreach (var action in updateActions.ToArray())
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void FixedUpdate()
        {
            foreach (var action in fixedUpdateActions.ToArray())
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void LateUpdate()
        {
            foreach (var action in lateUpdateActions.ToArray())
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private class LifetimeServiceHelper : MonoBehaviour
        {
            private LifetimeService _lifetimeService = null!;
            public void Initialize(LifetimeService lifetimeService)
            {
                _lifetimeService = lifetimeService;
            }

            void Update() => _lifetimeService.Update();
            void FixedUpdate() => _lifetimeService.FixedUpdate();
            void LateUpdate() => _lifetimeService.LateUpdate();
        }
    }
}