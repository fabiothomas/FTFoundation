using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FTFoundation.Core.Validation;

namespace FTFoundation.Core
{
    // Responsible for scanning assemblies and resolving which service implementations are active
    // for the current build profile and platform.
    internal static class ServiceCandidateResolver
    {
        internal struct ResolvedCandidates
        {
            // Single-winner per interface (may be a fallback if no profile-matched candidates exist)
            public Dictionary<Type, Type> Winners;

            // All profile-matched (non-fallback) concrete types per interface, ordered by priority
            public Dictionary<Type, List<Type>> AllMatched;

            // Winning concrete types decorated with [InstantiateOnStartup] that should be eagerly created
            public List<Type> EagerStartups;

            // Problems to be emitted by the caller after resolution
            public List<ProblemDetail> Warnings;
        }

        private struct ServiceCandidate
        {
            public Type ImplementationType;
            public ServiceAttribute ServiceAttribute;
            public BuildTargetProfile Profiles;
            public BuildTargetPlatform Platforms;
            public int Priority;
            public bool IsFallback;
        }

        internal static ResolvedCandidates Resolve(BuildTargetProfile currentProfile, BuildTargetPlatform currentPlatform)
        {
            var winners = new Dictionary<Type, Type>();
            var allMatched = new Dictionary<Type, List<Type>>();
            var eagerStartups = new List<Type>();
            var warnings = new List<ProblemDetail>();

            // ── First pass: collect every [Service]-decorated type grouped by interface ──────────────
            var allCandidates = new Dictionary<Type, List<ServiceCandidate>>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetCustomAttribute<ServiceAssemblyAttribute>() == null) continue;

                foreach (var t in assembly.GetTypes())
                {
                    ServiceAttribute svcAttr = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
                    if (svcAttr == null) continue;

                    ServiceCandidate candidate = new()
                    {
                        ImplementationType = t,
                        ServiceAttribute = svcAttr,
                        Profiles = t.GetCustomAttribute<ServiceBuildProfileAttribute>()?.Profiles ?? BuildTargetProfile.All,
                        Platforms = t.GetCustomAttribute<ServiceBuildPlatformAttribute>()?.Platforms ?? BuildTargetPlatform.All,
                        Priority = t.GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? 0,
                        IsFallback = t.GetCustomAttribute<ServiceFallbackAttribute>() != null
                    };

                    if (!allCandidates.TryGetValue(svcAttr.Interface, out var list))
                    {
                        list = new List<ServiceCandidate>();
                        allCandidates[svcAttr.Interface] = list;
                    }
                    list.Add(candidate);
                }
            }

            // ── Second pass: profile filter + conflict resolution per interface ────────────────────
            foreach (var (iface, candidates) in allCandidates)
            {
                var profileMatched = candidates
                  .Where(c => !c.IsFallback && c.Profiles.HasFlag(currentProfile) && (c.Platforms & currentPlatform) != 0)
                  .OrderByDescending(c => c.Priority)
                  .ToList();

                var fallbacks = candidates
                  .Where(c => c.IsFallback && c.Profiles.HasFlag(currentProfile) && (c.Platforms & currentPlatform) != 0)
                  .OrderByDescending(c => c.Priority)
                  .ToList();

                // Warn when multiple non-fallback candidates share the highest priority
                if (profileMatched.Count > 1)
                {
                    int topPriority = profileMatched[0].Priority;
                    var tied = profileMatched.Where(c => c.Priority == topPriority).ToList();
                    if (tied.Count > 1)
                    {
                        var names = string.Join(", ", tied.Select(c => c.ImplementationType.Name));
                        warnings.Add(new ProblemDetail(ProblemDetailType.WARNING, $"Multiple services for '{iface.Name}' share priority {topPriority} in profile '{currentProfile}': [{names}]. Using '{profileMatched[0].ImplementationType.Name}' for single injection."));
                    }
                }

                // All profile-matched types are available for IReadOnlyList<T> injection
                if (profileMatched.Count > 0)
                    allMatched[iface] = profileMatched.Select(c => c.ImplementationType).ToList();

                // Single-winner: best profile-matched → best fallback → nothing (interface skipped this build)
                ServiceCandidate winner = profileMatched.Count > 0
                  ? profileMatched[0]
                  : (fallbacks.Count > 0 ? fallbacks[0] : default);

                if (winner.ImplementationType == null) continue;

                winners[iface] = winner.ImplementationType;

                if (winner.ImplementationType.GetCustomAttributes(typeof(InstantiateOnStartupAttribute), inherit: true).Any())
                {
                    if (winner.ServiceAttribute.Type == ServiceType.SINGLETON) eagerStartups.Add(winner.ImplementationType);
                    else warnings.Add(new ProblemDetail(ProblemDetailType.WARNING, $"InstantiateOnStartupAttribute is not valid on {winner.ImplementationType.Name} because it is not a singleton service"));
                }
            }

            return new ResolvedCandidates
            {
                Winners = winners,
                AllMatched = allMatched,
                EagerStartups = eagerStartups,
                Warnings = warnings
            };
        }
    }
}
