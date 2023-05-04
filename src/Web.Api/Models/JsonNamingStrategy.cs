using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Serialization;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Ulearn.Web.Api.Models
{
	// https://dotnetcoretutorials.com/2018/05/05/setting-json-serialization-configuration-at-runtime-on-a-net-core-api/
	public class ApiHeaderJsonNamingStrategyOptions
	{
		public string HeaderName { get; set; }
		public Dictionary<string, NamingStrategy> NamingStrategies { get; set; }
		public NamingStrategy DefaultStrategy { get; set; }
		public Func<IHttpContextAccessor> HttpContextAccessorProvider { get; set; }
	}

	public class ApiHeaderJsonNamingStrategy : NamingStrategy
	{
		private readonly ApiHeaderJsonNamingStrategyOptions options;

		public ApiHeaderJsonNamingStrategy(ApiHeaderJsonNamingStrategyOptions options)
		{
			this.options = options;
		}

		public string GetValidNamingStrategyHeader()
		{
			var httpContext = options.HttpContextAccessorProvider().HttpContext;
			var namingStrategyHeader = httpContext!.Request.Headers[options.HeaderName].FirstOrDefault()?.ToLower();

			if (string.IsNullOrEmpty(namingStrategyHeader) || !options.NamingStrategies.ContainsKey(namingStrategyHeader))
				return string.Empty;
			return namingStrategyHeader;
		}

		protected override string ResolvePropertyName(string name)
		{
			var namingStrategyHeader = GetValidNamingStrategyHeader();
			var strategy = string.IsNullOrEmpty(namingStrategyHeader)
				? options.DefaultStrategy
				: options.NamingStrategies[namingStrategyHeader];

			//This is actually bit of a bastardization because we shouldn't really be calling this method here. 
			//We want to actually just call the "ResolvePropertyName" method, but it's protected. This essentially ends up doing the same thing. 
			return strategy.GetPropertyName(name, false);
		}
	}

	public class ApiHeaderJsonContractResolver : DefaultContractResolver
	{
		private readonly ApiHeaderJsonNamingStrategy apiHeaderJsonNamingStrategy;
		private readonly Func<IMemoryCache> memoryCacheProvider;

		public ApiHeaderJsonContractResolver(ApiHeaderJsonNamingStrategyOptions namingStrategyOptions, Func<IMemoryCache> memoryCacheProvider)
		{
			apiHeaderJsonNamingStrategy = new ApiHeaderJsonNamingStrategy(namingStrategyOptions);
			NamingStrategy = apiHeaderJsonNamingStrategy;
			this.memoryCacheProvider = memoryCacheProvider;
		}

		public override JsonContract ResolveContract(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			var cacheKey = apiHeaderJsonNamingStrategy.GetValidNamingStrategyHeader() + type;
			var contract = memoryCacheProvider().GetOrCreate(cacheKey, entry =>
			{
				entry.AbsoluteExpiration = DateTimeOffset.MaxValue;
				return CreateContract(type);
			});

			return contract;
		}
	}
}