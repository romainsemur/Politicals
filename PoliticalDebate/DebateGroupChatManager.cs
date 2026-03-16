using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace PoliticalDebate;

/// <summary>
/// Round-robin group chat manager for the political debate.
/// Cycles through agents in order: Journaliste -> Gauche -> Droite, repeated for 3 rounds.
/// </summary>
internal sealed class DebateGroupChatManager : GroupChatManager
{
    private readonly IReadOnlyList<AIAgent> _agents;

    public DebateGroupChatManager(IReadOnlyList<AIAgent> agents)
    {
        _agents = agents;
    }

    protected override ValueTask<AIAgent> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        // Round-robin: cycle through agents based on iteration count
        int agentIndex = IterationCount % _agents.Count;
        return new ValueTask<AIAgent>(_agents[agentIndex]);
    }
}
