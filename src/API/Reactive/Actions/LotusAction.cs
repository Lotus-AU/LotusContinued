using System;
using System.Linq;
using System.Reflection;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using VentLib.Utilities.Debug.Profiling;

namespace Lotus.API.Reactive.Actions;

public class LotusAction
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(LotusAction));

    public LotusActionType ActionType { get; }
    public Priority Priority { get; }

    internal LotusActionAttribute Attribute;
    internal MethodInfo Method;
    internal object Executer = null!;
    internal object ForcedExecutor = null!;

    public LotusAction(LotusActionAttribute attribute, MethodInfo method)
    {
        this.Method = method;
        this.ActionType = attribute.ActionType;
        this.Priority = attribute.Priority;
        this.Attribute = attribute;
    }

    public virtual void Execute(object[] args)
    {
        if (ForcedExecutor == null && Executer == null)
            throw new InvalidOperationException("Executer is not set.");

        var executer = (ForcedExecutor ?? Executer) as CustomRole;

        if (!(executer?.MyPlayer))
            throw new InvalidOperationException("MyPlayer is not set in the instance.");
        log.Trace($"RoleAction(type={ActionType}, executer={ForcedExecutor ?? Executer}, priority={Priority}, method={Method}))", "RoleAction::Execute");
        Profiler.Sample sample1 = Profilers.Global.Sampler.Sampled($"Action::{ActionType}");
        Profiler.Sample sample2 = Profilers.Global.Sampler.Sampled((Method.ReflectedType?.FullName ?? "") + "." + Method.Name);
        try
        {
            Method.Invoke(ForcedExecutor ?? Executer, args);
        }
        catch (TargetParameterCountException _)
        {
            var expectedParameters = Method.GetParameters();
            var actualParameters = args.Length;
            log.Exception($"Expected parameters: {expectedParameters.Length}, Actual parameters: {actualParameters}");

            log.Exception($"Expected parameter types: {string.Join(", ", expectedParameters.Select(p => p.ParameterType.Name))}");
            log.Exception($"Received arguments: {string.Join(", ", args.Select(a => a?.GetType().Name ?? "null"))}");
            log.Exception($"Parameter count mismatch: expected {Method.GetParameters().Length}, received {args.Length}");
            throw;
        }
        catch (ArgumentException _)
        {
            var expectedParameters = Method.GetParameters();
            var actualParameters = args.Length;
            log.Exception($"Expected parameters: {expectedParameters.Length}, Actual parameters: {actualParameters}");

            log.Exception($"Expected parameter types: {string.Join(", ", expectedParameters.Select(p => p.ParameterType.Name))}");
            log.Exception($"Received arguments: {string.Join(", ", args.Select(a => a?.GetType().Name ?? "null"))}");
            log.Exception($"Parameter count mismatch: expected {Method.GetParameters().Length}, received {args.Length}");
            throw;
        }
        catch (Exception ex)
        {
            log.Exception($"Error invoking method: {ex}");
            throw;
        }
        sample1.Stop();
        sample2.Stop();
    }

    public virtual void Execute(AbstractBaseRole role, object[] args)
    {
        if (role == null)
            throw new InvalidOperationException("Executer is not set and role is not provided.");

        log.Trace($"RoleAction(type={ActionType}, specificExecutor={role}, priority={Priority}, method={Method}))", "RoleAction::Execute");
        Profiler.Sample sample1 = Profilers.Global.Sampler.Sampled($"Action::{ActionType}");
        Profiler.Sample sample2 = Profilers.Global.Sampler.Sampled((Method.ReflectedType?.FullName ?? "") + "." + Method.Name);
        Method.InvokeAligned(role, args);
        sample1.Stop();
        sample2.Stop();
    }

    public virtual void ExecuteFixed(object? role = null)
    {
        if (ForcedExecutor == null && Executer == null && role == null)
            throw new InvalidOperationException("Executer is not set and role is not provided.");

        try
        {
            Method.Invoke(ForcedExecutor ?? Executer ?? role, null);
        }
        catch (Exception ex)
        {
            log.Exception($"Error invoking method: {ex}");
            throw;
        }
    }

    public void SetExecuter(object executer)
    {
        Executer = executer;
    }

    public void SetForcedExecuter(object forcedExec)
    {
        ForcedExecutor = forcedExec;
    }

    public LotusAction Clone()
    {
        return (LotusAction)this.MemberwiseClone();
    }

    public override string ToString() => $"LotusAction(type={ActionType}, executer={Executer}, priority={Priority}, method={Method}))";

}