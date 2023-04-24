﻿using System;
using Microsoft.Extensions.Options;
using Ulearn.Common;
using Vostok.Logging.Abstractions;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Utils.SuperGroup;

public class SuperGroupGoogleSheetCache : LruCache<string, (string groupName, string studentName)[]>, ISuperGroupGoogleSheetCache
{
	private static ILog log => LogProvider.Get().ForContext(typeof(SuperGroupGoogleSheetCache));

	public SuperGroupGoogleSheetCache(IOptions<WebApiConfiguration> options)
		: base(options.Value.SuperGroupCache.Capacity, TimeSpan.FromMinutes(options.Value.SuperGroupCache.MaxLifeTime.Minutes))
	{
		var capacity = options.Value.SuperGroupCache.Capacity;
		var maxLifeTime = options.Value.SuperGroupCache.MaxLifeTime;
		log.Info($"Creating auto group google sheet cache with capacity {capacity} and max life time {maxLifeTime.Minutes} minutes");
	}
}