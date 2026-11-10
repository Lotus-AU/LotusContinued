using System.Collections.Generic;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.Logging;
using Lotus.Managers.Templates.Models.Units;
using VentLib.Utilities.Debug.Profiling;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.Templates.Models;

// ReSharper disable once InconsistentNaming
public class TTrigger: TemplateUnit
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(TTrigger));

    public static Dictionary<string, Hook> BoundHooks = new();

    public List<string>? Events { get; set; }
    public bool ShownToEveryone { get; set; } = true;

    public Template Parent = null!;

    public void Setup(Template parent)
    {
        Parent = parent;
        Events?.ForEach(ev =>
        {
            string key = $"{nameof(TTrigger)}~{TemplateManager.GlobalTriggerCount++}";
            Hook? hook = TemplateTriggers.BindTrigger(key, ev, tr => RunTemplateCallback(ev, tr));
            if (hook != null)
            {
                log.Trace($"Successfully bound Template Trigger \"{key}\" for \"{ev}\"", "TemplateTriggers");
                BoundHooks[key] = hook;
            } else log.Trace($"Could not bind Template Trigger \"{key}\" for \"{ev}\"", "TemplateTriggers");
        });
    }

    private void RunTemplateCallback(string triggerName, ResolvedTrigger? result)
    {
        Profiler.Sample sample = Profilers.Global.Sampler.Sampled("TriggerCallback");
        if (result == null) log.Warn($"Unable to run call back for \"{triggerName}\". No valid resolvers found for trigger event.");
        else
        {
            MetaVariable = result.Data;
            DevLogger.Log($"Meta Variable: {MetaVariable}");
            if (result.Player) Triggerer = result.Player!.PlayerId;

            bool evaluateResult = Evaluate(result.Player);
            DevLogger.Log($"Result: {evaluateResult}");
            if (evaluateResult)
            {
                if (!ShownToEveryone && result.Player) Parent.SendMessage(result.Player!, result.Player, result.Player);
                else Players.GetPlayers().ForEach(p => Parent.SendMessage(p, p, p));
            }

            MetaVariable = null;
            Triggerer = byte.MaxValue;
        }
        sample.Stop();
    }
}
