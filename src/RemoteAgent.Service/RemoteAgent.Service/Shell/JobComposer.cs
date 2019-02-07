using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace RemoteAgent.Service.Shell
{
	public class JobComposer
	{
		private static readonly ILogger Logger = LogManager.GetLogger(nameof(ServiceCore));

		public IEnumerable<JobBase> Compose(CompositionContext context)
		{
			var jobTypes = typeof(JobComposer).Assembly.ExportedTypes.Where(d => typeof(JobBase).IsAssignableFrom(d) && !d.IsAbstract);
			var tasks = new List<JobBase>();
			foreach (var jobType in jobTypes)
			{
				var instance = ComposeType(jobType, context);
				tasks.Add(instance);
			}

			return tasks;
		}

		private static JobBase ComposeType(Type jobType, CompositionContext context)
		{
			Logger.Debug($"Composing type: {jobType.FullName}.");
			var constructors = jobType.GetConstructors();
			if (constructors.Length > 1)
			{
				Logger.Error($"More than one constructor detected.");
				throw new Exception($"More than one constructor detected.");
			}

			if (constructors.Length == 0)
			{
				Logger.Error($"No constructor available.");
			}

			var parameters = constructors[0].GetParameters();
			if (parameters.Length == 0)
			{
				try
				{
					return Activator.CreateInstance(jobType) as JobBase;
				}
				catch (Exception e)
				{
					Logger.Error(e, $"Failed to create type for {jobType.FullName}");
				}
				finally
				{
					Logger.Debug($"Job creation successful.");
				}
			}

			Logger.Debug($"Constructor requires {parameters.Length} parameters.");
			var serviceInstances = new object[parameters.Length];
			for (var paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
			{
				var parameterInfo = parameters[paramIndex];
				Logger.Debug($"Loading service for type {parameterInfo.ParameterType}.");
				var service = context.ServiceProvider.GetService(parameterInfo.ParameterType);
				serviceInstances[paramIndex] = service;
			}

			Logger.Debug($"Invoking constructor.");
			var instance = constructors[0].Invoke(serviceInstances);
			Logger.Debug($"Job creation successful.");
			return instance as JobBase;
		}
	}
}