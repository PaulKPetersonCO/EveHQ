// ==============================================================================
// 
// EveHQ - An Eve-Online™ character assistance application
// Copyright © 2005-2015  EveHQ Development Team
//   
// This file is part of EveHQ.
//  
// The source code for EveHQ is free and you may redistribute 
// it and/or modify it under the terms of the MIT License. 
// 
// Refer to the NOTICES file in the root folder of EVEHQ source
// project for details of 3rd party components that are covered
// under their own, separate licenses.
// 
// EveHQ is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
// license below for details.
// 
// ------------------------------------------------------------------------------
// 
// The MIT License (MIT)
// 
// Copyright © 2005-2015  EveHQ Development Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------
// 
// <copyright file="TextFileCacheProvider.cs" company="EveHQ Development Team">
//     Copyright © 2005-2015  EveHQ Development Team
// </copyright>
// 
// ==============================================================================

#region Usings

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using EveHQ.Common.Extensions;
using Newtonsoft.Json;

#endregion


namespace EveHQ.Caching
{
	/// <summary>
	/// A simple caching system that uses text files for persistence.
	/// </summary>
	public sealed class TextFileCacheProvider : ICacheProvider
	{
		public TextFileCacheProvider(string rootPath)
		{
			_rootPath = rootPath;
			if (!Directory.Exists(_rootPath))
			{
				Directory.CreateDirectory(_rootPath);
			}
		}

		public void Add<T>(string key, T value, DateTimeOffset cacheUntil)
		{
			var cacheitem = new CacheItem<T> { CacheUntil = cacheUntil, Data = value };

			_memCache[key] = cacheitem;
			SaveToFile(key, cacheitem);
		}

		public CacheItem<T> Get<T>(string key)
		{
			if (_memCache.TryGetValue(key, out object item))
			{
				return item as CacheItem<T>;
			}

			// not in memory, check disk
			item = GetFromDisk<T>(key);

			if (item == null)
			{
				return null; // data didn't exist in cache.
			}

			_memCache[key] = item;
			return (CacheItem<T>)item;
		}

		private CacheItem<T> GetFromDisk<T>(string key)
		{
			var fullPath = CreateCacheFilePath(key);

			if (!File.Exists(fullPath))
			{
				return null; // the file doesn't exist, therefore there's no cache.
			}

			using (var streamReader = new StreamReader(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
			{
				return JsonConvert.DeserializeObject<CacheItem<T>>(streamReader.ReadToEnd());
			}
		}

		private void SaveToFile<T>(string key, CacheItem<T> cacheitem)
		{
			try
			{
				using (var stream = new FileStream(CreateCacheFilePath(key), FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					var dataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cacheitem));
					stream.Write(dataBytes, 0, dataBytes.Length);
				}
			}
			catch (Exception e)
			{
				// supress the exception since we have the data in memory, but log the occurance
				Trace.TraceWarning(e.FormatException());
			}
		}

		private string CreateCacheFilePath(string key) => Path.Combine(_rootPath, $"{key}.json.txt");

		private readonly ConcurrentDictionary<string, object> _memCache = new ConcurrentDictionary<string, object>();
		private readonly string _rootPath;
	}
}