using System.Collections.Generic;
using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>Temporary contextual memory: what was seen, said and found.</summary>
    public interface IMemoryService
    {
        IReadOnlyList<ObservedObject> Objects { get; }
        IReadOnlyList<AssistantTurn> Turns { get; }
        ObservedObject Focus { get; }                 // the object most recently talked about

        ObservedObject Remember(Detection detection, Vector3 worldPosition, bool hasPosition);
        void RememberTurn(AssistantTurn turn);
        void RememberSearch(string objectId, SearchResult result);
        void SetFocus(ObservedObject obj);
        ObservedObject FindById(string id);
        ObservedObject FindByName(string nameFragment);
        /// <summary>Resolve references like "it", "that one", "the one next to it", "the second one".</summary>
        List<ObservedObject> ResolveReference(string userText, string languageCode, Vector3 userPosition, Vector3 gazeDirection);
        string BuildContext(int maxObjects, int maxTurns);
        void ClearContext();
        void ForgetCurrentContext();
        void ClearHistory();
    }
}
