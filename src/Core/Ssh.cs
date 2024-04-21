using System.Diagnostics;
using Serilog;

namespace Tirax.TunnelSpace;

public static class Ssh
{
    const string SshAgentProcessName = "ssh-agent";

    public static Unit Initialize(ILogger logger) {
        if (GetRunningSshAgents())
            logger.Information("Agent is already running");
        else {
            Process.Start(SshAgentProcessName);
            logger.Information("Agent started");
        }
        return unit;
    }

    static bool GetRunningSshAgents() {
        var agents = Process.GetProcesses();
        var states = from agent in agents
                     where agent.ProcessName == SshAgentProcessName
                     select (agent.Id, agent.Responding);
        return states.Any();
    }
}
