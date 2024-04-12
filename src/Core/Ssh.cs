using System.Diagnostics;
using Serilog;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace;

public static class Ssh
{
    const string SshAgentProcessName = "ssh-agent";

    static Eff<Seq<Process>> GetProcesses() =>
        Eff(() => Process.GetProcesses().ToSeq());

    static Eff<Unit> StartProcess(string path) =>
        Eff(() => {
                Process.Start(path);
                return unit;
            });

    static Eff<bool> GetRunningSshAgents() =>
        from agents in GetProcesses()
        let states = from agent in agents
                     where agent.ProcessName == SshAgentProcessName
                     select (agent.Id, agent.Responding)
        select states.Any();

    static Eff<Unit> StartSshIfNeeded(ILogger logger) =>
        from isRunning in GetRunningSshAgents()
        from message in isRunning ? SuccessEff("Agent is already running")
                            : StartProcess(SshAgentProcessName).Map(_ => "Agent started")
        from _ in logger.InformationEff("Agent running state: {State}", message)
        select unit;

    public static Eff<Unit> Initialize(ILogger logger) =>
        StartSshIfNeeded(logger);
}
